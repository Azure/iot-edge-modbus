namespace AzureIoTEdgeModbus.Slave
{
    using AzureIoTEdgeModbus.SerialDevice;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// This class is Modbus RTU session.
    /// </summary>
    public class ModbusRTUSlaveSession : ModbusSlaveSession
    {
        #region Constructors
        public ModbusRTUSlaveSession(ModbusSlaveConfig conf)
            : base(conf)
        {
        }
        #endregion

        #region Protected Properties
        protected override int m_reqSize => 8;
        protected override int m_dataBodyOffset => 1;
        protected override int m_silent => 100;
        #endregion

        #region Private Fields
        private const int m_numOfBits = 8;
        private ISerialDevice m_serialPort = null;
        #endregion

        #region Public Methods
        public override void ReleaseSession()
        {
            this.ReleaseOperations();
            if (this.m_serialPort != null)
            {
                this.m_serialPort.Dispose();
                this.m_serialPort = null;
            }
        }
        #endregion

        #region Private Methods
        protected override async Task ConnectSlave()
        {
            try
            {
                if (this.config.SlaveConnection.Substring(0, 3) == "COM" || this.config.SlaveConnection.Substring(0, 8) == "/dev/tty")
                {
                
                    Console.WriteLine($"Opening...{this.config.SlaveConnection}");

                    this.m_serialPort = SerialDeviceFactory.CreateSerialDevice(this.config.SlaveConnection, (int)this.config.BaudRate.Value, this.config.Parity.Value, this.config.DataBits.Value, this.config.StopBits.Value);

                    this.m_serialPort.Open();
                    //m_serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    await Task.Delay(2000); //Wait target to be ready to write the modbus package
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Connect Slave failed");
                Console.WriteLine(e.Message);
                this.m_serialPort = null;
            }
        }
        protected override void EncodeRead(ReadOperation operation)
        {
            //uid
            operation.Request[0] = operation.UnitId;

            //Body
            //function code
            operation.Request[this.m_dataBodyOffset] = operation.FunctionCode;
            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(operation.Address)));
            operation.Request[this.m_dataBodyOffset + 1] = address_byte[0];
            operation.Request[this.m_dataBodyOffset + 2] = address_byte[1];
            //count
            byte[] count_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)operation.Count));
            operation.Request[this.m_dataBodyOffset + 3] = count_byte[0];
            operation.Request[this.m_dataBodyOffset + 4] = count_byte[1];

            if (this.GetCRC(operation.Request, 6, out UInt16 crc))
            {
                byte[] crc_byte = BitConverter.GetBytes(crc);
                operation.Request[this.m_dataBodyOffset + 5] = crc_byte[0];
                operation.Request[this.m_dataBodyOffset + 6] = crc_byte[1];
            }
        }


        protected override void EncodeWrite(byte[] request, WriteOperation writeOperation)
        {
            //uid
            request[0] = Convert.ToByte(writeOperation.UnitId);

            //Body
            //function code
            request[m_dataBodyOffset] = writeOperation.FunctionCode;

            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(writeOperation.Address)));
            request[this.m_dataBodyOffset + 1] = address_byte[0];
            request[this.m_dataBodyOffset + 2] = address_byte[1];
            //value
            UInt16 value_int = writeOperation.IntValueToWrite;
            if (writeOperation.Address == '0' && value_int == 1)
            {
                request[this.m_dataBodyOffset + 3] = 0xFF;
                request[this.m_dataBodyOffset + 4] = 0x00;
            }
            else
            {
                byte[] val_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)value_int));
                request[this.m_dataBodyOffset + 3] = val_byte[0];
                request[this.m_dataBodyOffset + 4] = val_byte[1];
            }
            if (this.GetCRC(request, 6, out UInt16 crc))
            {
                byte[] crc_byte = BitConverter.GetBytes(crc);
                request[this.m_dataBodyOffset + 5] = crc_byte[0];
                request[this.m_dataBodyOffset + 6] = crc_byte[1];
            }
        }
        protected override async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            //double slient_interval = 1000 * 5 * ((double)1 / (double)config.BaudRate);
            byte[] response = null;

            this.m_semaphore_connection.Wait();

            if (this.m_serialPort != null && this.m_serialPort.IsOpen())
            {
                try
                {
                    this.m_serialPort.DiscardInBuffer();
                    this.m_serialPort.DiscardOutBuffer();
                    Task.Delay(this.m_silent).Wait();
                    this.m_serialPort.Write(request, 0, reqLen);
                    response = this.ReadResponse();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Something wrong with the connection, disposing...");
                    Console.WriteLine(e.Message);
                    this.m_serialPort.Dispose();
                    this.m_serialPort = null;
                    Console.WriteLine("Connection lost, reconnecting...");
                    await this.ConnectSlave();
                }
            }
            else
            {
                Console.WriteLine("Connection lost, reconnecting...");
                await this.ConnectSlave();
            }

            this.m_semaphore_connection.Release();

            return response;
        }
        private byte[] ReadResponse()
        {
            byte[] response = new byte[m_bufSize];
            int header_len = 0;
            int data_len = 0;
            int h_l = 0;
            int d_l = 0;
            int retry = 0;

            while (header_len < 3 && retry < this.config.RetryCount)
            {
                h_l = this.m_serialPort.Read(response, header_len, 3 - header_len);
                if (h_l > 0)
                {
                    header_len += h_l;
                }
                else
                {
                    retry++;
                    Task.Delay(this.config.RetryInterval.Value).Wait();
                }
            }

            int byte_counts = response[1] >= ModbusExceptionCode ? 2 : response[2] + 2;
            while (data_len < byte_counts && retry < this.config.RetryCount)
            {
                d_l = this.m_serialPort.Read(response, 3 + data_len, byte_counts - data_len);
                if (d_l > 0)
                {
                    data_len += d_l;
                }
                else
                {
                    retry++;
                    Task.Delay(this.config.RetryInterval.Value).Wait();
                }
            }

            if (retry >= this.config.RetryCount)
            {
                response = null;
            }

            return response;
        }
        //private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    string data = m_serialPort.ReadExisting();
        //}
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

                for (int _bit = 0; _bit < m_numOfBits; ++_bit)
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
        #endregion
    }
}
