namespace AzureIoTEdgeModbus.Slave
{
    using System;
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

        public override void ReleaseSession()
        {
            this.ReleaseOperations();
            if (this.socket != null)
            {
                this.socket.Disconnect(false);
                this.socket.Dispose();
                this.socket = null;
            }
        }

        protected override async Task ConnectSlave()
        {
            if (IPAddress.TryParse(this.config.SlaveConnection, out this.address))
            {
                try
                {
                    this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = 100
                    };

                    await this.socket.ConnectAsync(this.address, this.config.TcpPort.Value);

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
            byte[] addressBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(operation.Address));
            operation.Request[this.FunctionCodeOffset + 1] = addressBytes[0];
            operation.Request[this.FunctionCodeOffset + 2] = addressBytes[1];
            //count
            byte[] countBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(operation.Decoder.GetByteCount(operation.Count)));
            operation.Request[this.FunctionCodeOffset + 3] = countBytes[0];
            operation.Request[this.FunctionCodeOffset + 4] = countBytes[1];
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
            request[FunctionCodeOffset] = (byte)writeOperation.FunctionCode;

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
        protected override async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            byte[] response = null;
            byte[] garbage = new byte[BufferSize];
            int retryForSocketError = 0;
            bool sendSucceed = false;

            while (!sendSucceed && retryForSocketError < this.config.RetryCount)
            {
                retryForSocketError++;
                this.semaphoreConnection.Wait();

                if (this.socket != null && this.socket.Connected)
                {
                    try
                    {
                        // clear receive buffer
                        while (this.socket.Available > 0)
                        {
                            int rec = this.socket.Receive(garbage, BufferSize, SocketFlags.None);
                            Console.WriteLine("Dumping socket receive buffer...");
                            string data = "";
                            int cnt = 0;
                            while (cnt < rec)
                            {
                                data += garbage[cnt].ToString();
                                cnt++;
                            }
                            Console.WriteLine(data);
                        }

                        // send request
                        this.socket.Send(request, reqLen, SocketFlags.None);

                        // read response
                        response = this.ReadResponse();
                        sendSucceed = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something wrong with the socket, disposing...");
                        Console.WriteLine(e.Message);
                        this.socket.Disconnect(false);
                        this.socket.Dispose();
                        this.socket = null;
                        Console.WriteLine("Connection lost, reconnecting...");
                        await this.ConnectSlave();
                    }
                }
                else
                {
                    Console.WriteLine("Connection lost, reconnecting...");
                    await this.ConnectSlave();
                }

                this.semaphoreConnection.Release();
            }

            return response;
        }

        private byte[] ReadResponse()
        {
            byte[] response = new byte[BufferSize];
            int totalHeaderBytesRead = 0;
            int totalDataBytesRead = 0;
            int retry = 0;
            bool error = false;

            while (this.socket.Available <= 0 && retry < this.config.RetryCount)
            {
                retry++;
                Task.Delay(this.config.RetryInterval.Value).Wait();
            }

            while (totalHeaderBytesRead < this.FunctionCodeOffset && retry < this.config.RetryCount)
            {
                if (this.socket.Available > 0)
                {
                    var headerBytesRead = this.socket.Receive(response, totalHeaderBytesRead, this.FunctionCodeOffset - totalHeaderBytesRead, SocketFlags.None);
                    if (headerBytesRead > 0)
                    {
                        totalHeaderBytesRead += headerBytesRead;
                    }
                }
                else
                {
                    error = true;
                    break;
                }
            }

            var bytesToRead = IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(response, 4)) - 1;

            while (totalDataBytesRead < bytesToRead && retry < this.config.RetryCount)
            {
                if (this.socket.Available > 0)
                {
                    var dataBytesRead = this.socket.Receive(response, this.FunctionCodeOffset + totalDataBytesRead, bytesToRead - totalDataBytesRead, SocketFlags.None);
                    if (dataBytesRead > 0)
                    {
                        totalDataBytesRead += dataBytesRead;
                    }
                }
                else
                {
                    error = true;
                    break;
                }
            }

            if (retry >= this.config.RetryCount || error)
            {
                response = null;
            }

            return response;
        }
    }
}
