namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// This class is Modbus TCP session.
    /// </summary>
    public class ModbusTCPSlaveSession : ModbusSlaveSession
    {
        public ModbusTCPSlaveSession(ModbusSlaveConfig conf)
            : base(conf)
        {
        }

        protected override int RequestSize => 12;
        protected override int FunctionCodeOffset => 7;
        protected override int Silent => 0;

        private Socket socket;
        private IPAddress address;

        public override async Task ReleaseSessionAsync()
        {
            await this.ReleaseOperationsAsync().ConfigureAwait(false);
            if (this.socket != null)
            {
                this.socket.Disconnect(false);
                this.socket.Dispose();
                this.socket = null;
            }
        }

        protected override async Task ConnectSlaveAsync()
        {
            if (IPAddress.TryParse(this.config.SlaveConnection, out this.address))
            {
                try
                {
                    this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = 100
                    };

                    await this.socket.ConnectAsync(this.address, this.config.TcpPort).ConfigureAwait(false);

                }
                catch (SocketException se)
                {
                    Console.WriteLine("Connection to slave failed.");
                    Console.WriteLine(se.Message);
                    this.socket = null;
                }
            }
        }

        protected override void EncodeRead(ReadOperation operation)
        {
            //MBAP
            //transaction id 2 bytes
            operation.Request[0] = 0;
            operation.Request[1] = 0;
            //protocol id 2 bytes
            operation.Request[2] = 0;
            operation.Request[3] = 0;
            //length
            const int requestLength = 6;
            byte[] requestLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)requestLength));
            operation.Request[4] = requestLengthBytes[0];
            operation.Request[5] = requestLengthBytes[1];
            //uid
            operation.Request[6] = operation.UnitId;

            //Body
            //function code
            operation.Request[this.FunctionCodeOffset] = (byte)operation.FunctionCode;
            //address
            byte[] addressBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)operation.Address));
            operation.Request[this.FunctionCodeOffset + 1] = addressBytes[0];
            operation.Request[this.FunctionCodeOffset + 2] = addressBytes[1];
            //count
            byte[] entityCountBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(operation.Decoder.GetEntityCount(operation.Count)));
            operation.Request[this.FunctionCodeOffset + 3] = entityCountBytes[0];
            operation.Request[this.FunctionCodeOffset + 4] = entityCountBytes[1];
        }

        protected override void EncodeWrite(byte[] request, WriteOperation writeOperation)
        {
            //MBAP
            //transaction id 2 bytes
            request[0] = 0;
            request[1] = 0;
            //protocol id 2 bytes
            request[2] = 0;
            request[3] = 0;
            //length
            const int requestLength = 6;
            byte[] requestLengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)requestLength));
            request[4] = requestLengthBytes[0];
            request[5] = requestLengthBytes[1];
            //uid
            request[6] = Convert.ToByte(writeOperation.UnitId);

            //Body
            //function code
            request[this.FunctionCodeOffset] = (byte)writeOperation.FunctionCode;

            //address
            byte[] addressBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(writeOperation.Address)));
            request[this.FunctionCodeOffset + 1] = addressBytes[0];
            request[this.FunctionCodeOffset + 2] = addressBytes[1];
            
            //value
            UInt16 valueToWrite = writeOperation.IntValueToWrite;
            if (writeOperation.Entity == '0' && valueToWrite == 1)
            {
                request[this.FunctionCodeOffset + 3] = 0xFF;
                request[this.FunctionCodeOffset + 4] = 0x00;
            }
            else
            {
                byte[] valueBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)valueToWrite));
                request[this.FunctionCodeOffset + 3] = valueBytes[0];
                request[this.FunctionCodeOffset + 4] = valueBytes[1];
            }
        }
        protected override async Task<byte[]> SendRequestAsync(byte[] request)
        {
            byte[] response = null;
            var retryForSocketError = 0;
            var sendSucceed = false;

            while (!sendSucceed && retryForSocketError < this.config.RetryCount)
            {
                retryForSocketError++;
                await this.semaphoreConnection.WaitAsync().ConfigureAwait(false);

                if (this.socket != null && this.socket.Connected)
                {
                    try
                    {
                        // send request
                        await this.socket.SendAsync(request, SocketFlags.None).ConfigureAwait(false);
                        // read response
                        response = await this.ReadResponseAsync().ConfigureAwait(false);
                        sendSucceed = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something wrong with the socket, disposing...");
                        Console.WriteLine(e.GetType() + e.Message);
                        this.socket.Disconnect(false);
                        this.socket.Dispose();
                        this.socket = null;
                        Console.WriteLine("Connection lost, reconnecting...");
                        await this.ConnectSlaveAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    Console.WriteLine("Connection lost, reconnecting...");
                    await this.ConnectSlaveAsync().ConfigureAwait(false);
                }

                this.semaphoreConnection.Release();
            }

            return response;
        }

        private async Task<byte[]> ReadResponseAsync()
        {
            byte[] headerResponse = await this.ReadMBAPHeaderResponseAsync().ConfigureAwait(false);
            
            const int bytesInMbapHeaderAfterCountByte = 1;
            const int mbapHeaderByteCountStartIndex = 4;
            var bytesToRead = IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(headerResponse, mbapHeaderByteCountStartIndex)) - bytesInMbapHeaderAfterCountByte;

            var buffer = new byte[bytesToRead];
            var dataBytesRead = await this.socket.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false);

            if (dataBytesRead < bytesToRead)
            {
                Console.WriteLine($"Expected to read {bytesToRead} but read {dataBytesRead} bytes.");
            }

            return headerResponse.Concat(buffer).ToArray();
        }

        private async Task<byte[]> ReadMBAPHeaderResponseAsync()
        {
            const int mbapHeaderLength = 7;

            byte[] headerResponse = new byte[mbapHeaderLength];

            await this.socket.ReceiveAsync(headerResponse, SocketFlags.None).ConfigureAwait(false);

            return headerResponse;
        }
    }
}
