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
        #region Constructors
        public ModbusTCPSlaveSession(ModbusSlaveConfig conf)
            : base(conf)
        {
        }
        #endregion

        #region Protected Properties
        protected override int m_reqSize => 12;
        protected override int m_dataBodyOffset => 7;
        protected override int m_silent => 0;
        #endregion

        #region Private Fields
        private Socket m_socket = null;
        private IPAddress m_address = null;
        #endregion

        #region Public Methods
        public override void ReleaseSession()
        {
            this.ReleaseOperations();
            if (this.m_socket != null)
            {
                this.m_socket.Disconnect(false);
                this.m_socket.Dispose();
                this.m_socket = null;
            }
        }
        #endregion

        #region Protected Methods
        protected override async Task ConnectSlave()
        {
            if (IPAddress.TryParse(this.config.SlaveConnection, out this.m_address))
            {
                try
                {
                    this.m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = 100
                    };

                    await this.m_socket.ConnectAsync(this.m_address, this.config.TcpPort.Value);

                }
                catch (SocketException se)
                {
                    Console.WriteLine("Connection to slave failed.");
                    Console.WriteLine(se.Message);
                    this.m_socket = null;
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
            byte[] len_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)6));
            operation.Request[4] = len_byte[0];
            operation.Request[5] = len_byte[1];
            //uid
            operation.Request[6] = operation.UnitId;

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
        }
        protected override void EncodeWrite(byte[] request, string uid, ReadOperation readOperation, string value)
        {
            //MBAP
            //transaction id 2 bytes
            request[0] = 0;
            request[1] = 0;
            //protocol id 2 bytes
            request[2] = 0;
            request[3] = 0;
            //length
            byte[] len_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)6));
            request[4] = len_byte[0];
            request[5] = len_byte[1];
            //uid
            request[6] = Convert.ToByte(uid);

            //Body
            //function code

            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(readOperation.Address)));
            request[this.m_dataBodyOffset + 1] = address_byte[0];
            request[this.m_dataBodyOffset + 2] = address_byte[1];
            //value
            UInt16 value_int = (UInt16)Convert.ToInt32(value);
            if (readOperation.Entity == '0' && value_int == 1)
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
        }
        protected override async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            byte[] response = null;
            byte[] garbage = new byte[m_bufSize];
            int retryForSocketError = 0;
            bool sendSucceed = false;

            while (!sendSucceed && retryForSocketError < this.config.RetryCount)
            {
                retryForSocketError++;
                this.m_semaphore_connection.Wait();

                if (this.m_socket != null && this.m_socket.Connected)
                {
                    try
                    {
                        // clear receive buffer
                        while (this.m_socket.Available > 0)
                        {
                            int rec = this.m_socket.Receive(garbage, m_bufSize, SocketFlags.None);
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
                        this.m_socket.Send(request, reqLen, SocketFlags.None);

                        // read response
                        response = this.ReadResponse();
                        sendSucceed = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something wrong with the socket, disposing...");
                        Console.WriteLine(e.Message);
                        this.m_socket.Disconnect(false);
                        this.m_socket.Dispose();
                        this.m_socket = null;
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
            }

            return response;
        }
        #endregion

        #region Private Methods
        private byte[] ReadResponse()
        {
            byte[] response = new byte[m_bufSize];
            int header_len = 0;
            int data_len = 0;
            int h_l = 0;
            int d_l = 0;
            int retry = 0;
            bool error = false;

            while (this.m_socket.Available <= 0 && retry < this.config.RetryCount)
            {
                retry++;
                Task.Delay(this.config.RetryInterval.Value).Wait();
            }

            while (header_len < this.m_dataBodyOffset && retry < this.config.RetryCount)
            {
                if (this.m_socket.Available > 0)
                {
                    h_l = this.m_socket.Receive(response, header_len, this.m_dataBodyOffset - header_len, SocketFlags.None);
                    if (h_l > 0)
                    {
                        header_len += h_l;
                    }
                }
                else
                {
                    error = true;
                    break;
                }
            }

            int byte_counts = IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(response, 4)) - 1;

            while (data_len < byte_counts && retry < this.config.RetryCount)
            {
                if (this.m_socket.Available > 0)
                {
                    d_l = this.m_socket.Receive(response, this.m_dataBodyOffset + data_len, byte_counts - data_len, SocketFlags.None);
                    if (d_l > 0)
                    {
                        data_len += d_l;
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
        #endregion
    }
}
