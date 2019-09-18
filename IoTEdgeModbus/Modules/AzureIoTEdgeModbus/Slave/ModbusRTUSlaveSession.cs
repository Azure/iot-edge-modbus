namespace AzureIoTEdgeModbus.Slave
{
    using SerialDevice;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using static Constants;

    /// <summary>
    /// This class is Modbus RTU session.
    /// </summary>
    public class ModbusRTUSlaveSession : ModbusSlaveSession
    {
        public ModbusRTUSlaveSession(ModbusSlaveConfig conf)
            : base(conf)
        {
        }

        protected override int RequestSize => 8;
        protected override int FunctionCodeOffset => 1;
        protected override int Silent => 100;
        
        private ISerialDevice serialPort;

        public override async Task ReleaseSessionAsync()
        {
            await this.ReleaseOperationsAsync().ConfigureAwait(false);
            if (this.serialPort != null)
            {
                this.serialPort.Dispose();
                this.serialPort = null;
            }
        }

        protected override async Task ConnectSlaveAsync()
        {
            try
            {
                if (this.config.SlaveConnection.Substring(0, 3) == "COM" || this.config.SlaveConnection.Substring(0, 8) == "/dev/tty")
                {
                
                    Console.WriteLine($"Opening...{this.config.SlaveConnection}");

                    this.serialPort = SerialDeviceFactory.CreateSerialDevice(this.config.SlaveConnection, (int)this.config.BaudRate.Value, this.config.Parity.Value, this.config.DataBits.Value, this.config.StopBits.Value);

                    this.serialPort.Open();
                    //serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    await Task.Delay(2000).ConfigureAwait(false); //Wait target to be ready to write the modbus package
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Connect Slave failed");
                Console.WriteLine(e.Message);
                this.serialPort = null;
            }
        }
        protected override void EncodeRead(ReadOperation operation)
        {
            //uid
            operation.Request[0] = operation.UnitId;

            //Body
            //function code
            operation.Request[this.FunctionCodeOffset] = (byte)operation.FunctionCode;
            //address
            byte[] addressBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(operation.Address)));
            operation.Request[this.FunctionCodeOffset + 1] = addressBytes[0];
            operation.Request[this.FunctionCodeOffset + 2] = addressBytes[1];
            //count
            byte[] entityCountBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(operation.Decoder.GetEntityCount(operation.Count)));
            operation.Request[this.FunctionCodeOffset + 3] = entityCountBytes[0];
            operation.Request[this.FunctionCodeOffset + 4] = entityCountBytes[1];

            if (this.GetCRC(operation.Request, 6, out UInt16 crc))
            {
                byte[] crcBytes = BitConverter.GetBytes(crc);
                operation.Request[this.FunctionCodeOffset + 5] = crcBytes[0];
                operation.Request[this.FunctionCodeOffset + 6] = crcBytes[1];
            }
        }


        protected override void EncodeWrite(byte[] request, WriteOperation writeOperation)
        {
            //uid
            request[0] = Convert.ToByte(writeOperation.UnitId);

            //Body
            //function code
            request[FunctionCodeOffset] = (byte)writeOperation.FunctionCode;

            //address
            byte[] addressByte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(writeOperation.Address)));
            request[this.FunctionCodeOffset + 1] = addressByte[0];
            request[this.FunctionCodeOffset + 2] = addressByte[1];
            //value
            UInt16 valueToWrite = writeOperation.IntValueToWrite;
            if (writeOperation.Address == '0' && valueToWrite == 1)
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
            if (this.GetCRC(request, 6, out UInt16 crc))
            {
                byte[] crcBytes = BitConverter.GetBytes(crc);
                request[this.FunctionCodeOffset + 5] = crcBytes[0];
                request[this.FunctionCodeOffset + 6] = crcBytes[1];
            }
        }

        protected override async Task<byte[]> SendRequestAsync(byte[] request)
        {
            byte[] response = null;

            await this.semaphoreConnection.WaitAsync().ConfigureAwait(false);

            if (this.serialPort != null && this.serialPort.IsOpen())
            {
                try
                {
                    this.serialPort.DiscardInBuffer();
                    this.serialPort.DiscardOutBuffer();
                    await Task.Delay(this.Silent).ConfigureAwait(false);
                    this.serialPort.Write(request, 0, request.Length);
                    response = await this.ReadResponseAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Something wrong with the connection, disposing...");
                    Console.WriteLine(e.Message);
                    this.serialPort.Dispose();
                    this.serialPort = null;
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

            return response;
        }
        private async Task<byte[]> ReadResponseAsync()
        {
            byte[] response = new byte[BufferSize];
            int totalHeaderBytesRead = 0;
            int totalDataBytesRead = 0;
            int retry = 0;

            while (totalHeaderBytesRead < 3 && retry < this.config.RetryCount)
            {
                var headerBytesRead = this.serialPort.Read(response, totalHeaderBytesRead, 3 - totalHeaderBytesRead);
                if (headerBytesRead > 0)
                {
                    totalHeaderBytesRead += headerBytesRead;
                }
                else
                {
                    retry++;
                    await Task.Delay(this.config.RetryInterval).ConfigureAwait(false);
                }
            }

            var bytesToRead = response[1] >= ModbusExceptionCode ? 2 : response[2] + 2;

            while (totalDataBytesRead < bytesToRead && retry < this.config.RetryCount)
            {
                var dataBytesRead = this.serialPort.Read(response, 3 + totalDataBytesRead, bytesToRead - totalDataBytesRead);
                if (dataBytesRead > 0)
                {
                    totalDataBytesRead += dataBytesRead;
                }
                else
                {
                    retry++;
                    await Task.Delay(this.config.RetryInterval).ConfigureAwait(false);
                }
            }

            if (retry >= this.config.RetryCount)
            {
                response = null;
            }

            return response;
        }

        private bool GetCRC(byte[] message, int length, out UInt16 res)
        {
            UInt16 crcFull = 0xFFFF;
            res = 0;

            if (message == null || length <= 0)
            {
                return false;
            }

            for (int _byte = 0; _byte < length; ++_byte)
            {
                crcFull = (UInt16)(crcFull ^ message[_byte]);

                for (int bit = 0; bit < BitsInByte; ++bit)
                {
                    byte crcLsb = (byte)(crcFull & 0x0001);
                    crcFull = (UInt16)((crcFull >> 1) & 0x7FFF);

                    if (crcLsb == 1)
                    {
                        crcFull = (UInt16)(crcFull ^ 0xA001);
                    }
                }
            }
            res = crcFull;
            return true;
        }
    }
}
