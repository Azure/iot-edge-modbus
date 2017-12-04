namespace Modbus.Slaves
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.IO.Ports;

    /* Modbus Frame Details
     ----------------------- --------
    |MBAP Header description|Length  |
     ----------------------- --------
    |Transaction Identifier |2 bytes |
     ----------------------- --------
    |Protocol Identifier    |2 bytes |
     ----------------------- --------
    |Length 2bytes          |2 bytes |
     ----------------------- --------
    |Unit Identifier        |1 byte  |
     ----------------------- --------
    |Body                   |variable|
     ----------------------- --------
    */

    /// <summary>
    /// This class contains the handle for this module. In this case, it is a list of active Modbus sessions.
    /// </summary>
    class ModuleHandle
    {
        public static async Task<ModuleHandle> CreateHandleFromConfiguration(ModuleConfig config, ModbusSlaveSession.HandleResultDelegate resultHandler)
        {
            Modbus.Slaves.ModuleHandle moduleHandle = null;
            foreach (var config_pair in config.SlaveConfigs)
            {
                ModbusSlaveConfig slaveConfig = config_pair.Value;
                switch (slaveConfig.GetConnectionType())
                {
                    case ModbusConstants.ConnectionType.ModbusTCP:
                        {
                            if (moduleHandle == null)
                            {
                                moduleHandle = new Modbus.Slaves.ModuleHandle();
                            }

                            ModbusSlaveSession slave = new ModbusTCPSlaveSession(slaveConfig, resultHandler);
                            await slave.InitSession();
                            moduleHandle.ModbusSessionList.Add(slave);
                            break;
                        }
                    case ModbusConstants.ConnectionType.ModbusRTU:
                        {
                            if (moduleHandle == null)
                            {
                                moduleHandle = new Modbus.Slaves.ModuleHandle();
                            }

                            ModbusSlaveSession slave = new ModbusRTUSlaveSession(slaveConfig, resultHandler);
                            await slave.InitSession();
                            moduleHandle.ModbusSessionList.Add(slave);
                            break;
                        }
                    case ModbusConstants.ConnectionType.ModbusASCII:
                        {
                            break;
                        }
                    case ModbusConstants.ConnectionType.Unknown:
                        {
                            break;
                        }
                }
            }
            return moduleHandle;
        }
        public List<ModbusSlaveSession> ModbusSessionList = new List<ModbusSlaveSession>();
        public ModbusSlaveSession GetSlaveSession(string hwid)
        {
            return ModbusSessionList.Find(x => x.config.HwId.ToUpper() == hwid.ToUpper());
        }
        public void Release()
        {
            foreach (var session in ModbusSessionList)
            {
                session.ReleaseSession();
            }
            ModbusSessionList.Clear();
        }
    }

    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    abstract class ModbusSlaveSession
    {
        public ModbusSlaveConfig config;
        protected HandleResultDelegate messageDelegate;
        protected const int m_bufSize = 512;
        protected bool m_run = false;
        protected List<Task> m_taskList = new List<Task>();
        protected virtual int m_reqSize { get; }
        protected virtual int m_dataBodyOffset { get; }

        #region Constructors
        public ModbusSlaveSession(ModbusSlaveConfig conf, HandleResultDelegate resultHandler)
        {
            config = conf;
            messageDelegate = resultHandler;
        }
        #endregion

        #region Public Methods
        public abstract void ReleaseSession();
        public delegate void HandleResultDelegate(ModbusOutMessage message);
        public async Task InitSession()
        {
            await ConnectSlave();

            foreach (var op_pair in config.Operations)
            {
                ReadOperation x = op_pair.Value;
                //parse StartAddress to get address, function code and entity type
                ParseEntity(x.StartAddress, true, out ushort address_int16, out byte function_code, out byte entity_type);
                x.EntityType = entity_type;
                x.Address = address_int16;
                x.FunctionCode = function_code;
                //output format
                if (x.StartAddress.Length == 5)
                    x.OutFormat = "{0}{1:0000}";
                else if (x.StartAddress.Length == 6)
                    x.OutFormat = "{0}{1:00000}";

                x.RequestLen = m_reqSize;
                x.Request = new byte[m_bufSize];

                EncodeRead(x);
            }
        }
        public async Task WriteCB(string uid, string address, string value)
        {
            byte[] writeRequest = new byte[m_bufSize];
            byte[] writeResponse = null;
            int reqLen = m_reqSize;

            EncodeWrite(writeRequest, uid, address, value);
            writeResponse = await SendRequest(writeRequest, reqLen);
        }
        public void ProcessOperations()
        {
            m_run = true;
            foreach (var op_pair in config.Operations)
            {
                ReadOperation x = op_pair.Value;
                Task t = Task.Run(async () => await SingleOperation(x));
                m_taskList.Add(t);
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void EncodeWrite(byte[] request, string uid, string address, string value);
        protected abstract Task<byte[]> SendRequest(byte[] request, int reqLen);
        protected abstract Task ConnectSlave();
        protected abstract void EncodeRead(ReadOperation operation);
        protected async Task SingleOperation(ReadOperation x)
        {
            while (m_run)
            {
                x.Response = null;
                x.Response = await SendRequest(x.Request, x.RequestLen);

                if (x.Response != null)
                {
                    if (x.Request[m_dataBodyOffset] == x.Response[m_dataBodyOffset])
                    {
                        ProcessResponse(config, x);
                    }
                    else if (x.Request[m_dataBodyOffset] + 0x80 == x.Response[m_dataBodyOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {x.Response[m_dataBodyOffset + 1]}");
                    }
                }
                await Task.Delay(x.PollingInterval);
            }
        }
        protected void ProcessResponse(ModbusSlaveConfig config, ReadOperation x)
        {
            int count = 0;
            int step_size = 0;
            int start_digit = 0;
            switch (x.Response[m_dataBodyOffset])//function code
            {
                case (byte)ModbusConstants.FunctionCodeType.ReadCoils:
                case (byte)ModbusConstants.FunctionCodeType.ReadInputs:
                    {
                        count = x.Response[m_dataBodyOffset + 1] * 8;
                        count = (count > x.Count) ? x.Count : count;
                        step_size = 1;
                        start_digit = x.Response[m_dataBodyOffset] - 1;
                        break;
                    }
                case (byte)ModbusConstants.FunctionCodeType.ReadHoldingRegisters:
                case (byte)ModbusConstants.FunctionCodeType.ReadInputRegisters:
                    {
                        count = x.Response[m_dataBodyOffset + 1];
                        step_size = 2;
                        start_digit = (x.Response[m_dataBodyOffset] == 3) ? 4 : 3;
                        break;
                    }
            }
            for (int i = 0; i < count; i += step_size)
            {
                string res = "";
                string cell = "";
                string val = "";
                if (step_size == 1)
                {
                    cell = string.Format(x.OutFormat, (char)x.EntityType, x.Address + i + 1);
                    val = string.Format("{0}", (x.Response[m_dataBodyOffset + 2 + (i / 8)] >> (i % 8)) & 0b1);
                }
                else if (step_size == 2)
                {
                    cell = string.Format(x.OutFormat, (char)x.EntityType, x.Address + (i / 2) + 1);
                    val = string.Format("{0,00000}", ((x.Response[m_dataBodyOffset + 2 + i]) * 0x100 + x.Response[m_dataBodyOffset + 3 + i]));
                }
                res = cell + ": " + val + "\n";
                Console.WriteLine(res);

                ModbusOutMessage message = new ModbusOutMessage()
                { HwId = config.HwId, DisplayName = x.DisplayName, Address = cell, Value = val, SourceTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                messageDelegate(message);
            }
        }
        protected bool ParseEntity(string startAddress, bool isRead, out ushort outAddress, out byte functionCode, out byte entityType)
        {
            outAddress = 0;
            functionCode = 0;

            byte[] entity_type = Encoding.ASCII.GetBytes(startAddress, 0, 1);
            entityType = entity_type[0];
            string address_str = startAddress.Substring(1);
            int address_int = Convert.ToInt32(address_str);

            //function code
            switch ((char)entityType)
            {
                case (char)ModbusConstants.EntityType.CoilStatus:
                    {
                        functionCode = (byte)(isRead ? ModbusConstants.FunctionCodeType.ReadCoils : ModbusConstants.FunctionCodeType.WriteCoil);
                        break;
                    }
                case (char)ModbusConstants.EntityType.InputStatus:
                    {
                        if (isRead)
                            functionCode = (byte)ModbusConstants.FunctionCodeType.ReadInputs;
                        else
                            return false;
                        break;
                    }
                case (char)ModbusConstants.EntityType.InputRegister:
                    {
                        if (isRead)
                            functionCode = (byte)ModbusConstants.FunctionCodeType.ReadInputRegisters;
                        else
                            return false;
                        break;
                    }
                case (char)ModbusConstants.EntityType.HoldingRegister:
                    {
                        functionCode = (byte)(isRead ? ModbusConstants.FunctionCodeType.ReadHoldingRegisters : ModbusConstants.FunctionCodeType.WriteHoldingRegister);
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }
            //address
            outAddress = (UInt16)(address_int - 1);
            return true;
        }
        protected void ReleaseOperations()
        {
            m_run = false;
            Task.WaitAll(m_taskList.ToArray());
            m_taskList.Clear();
        }
        #endregion
    }

    /// <summary>
    /// This class is Modbus TCP session.
    /// </summary>
    class ModbusTCPSlaveSession : ModbusSlaveSession
    {
        #region Constructors
        public ModbusTCPSlaveSession(ModbusSlaveConfig conf, HandleResultDelegate resultHandler)
            : base(conf, resultHandler)
        {
        }
        #endregion

        #region Protected Properties
        protected override int m_reqSize { get { return 12; } }
        protected override int m_dataBodyOffset { get { return 7; } }
        #endregion

        #region Private Fields
        private const int m_tcpPort = 502;
        private object m_socketLock = new object();
        private Socket m_socket = null;
        private IPAddress m_address = null;
        #endregion

        #region Public Methods
        public override void ReleaseSession()
        {
            ReleaseOperations();
            if (m_socket != null)
            {
                m_socket.Disconnect(false);
                m_socket.Dispose();
                m_socket = null;
            }
        }
        #endregion

        #region Private Methods
        protected override async Task ConnectSlave()
        {
            if (IPAddress.TryParse(config.SlaveConnection, out m_address))
            {
                try
                {
                    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await m_socket.ConnectAsync(m_address, m_tcpPort);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connect Slave failed");
                    Console.WriteLine(e.Message);
                    m_socket = null;
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
            operation.Request[m_dataBodyOffset] = operation.FunctionCode;
            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(operation.Address)));
            operation.Request[m_dataBodyOffset + 1] = address_byte[0];
            operation.Request[m_dataBodyOffset + 2] = address_byte[1];
            //count
            byte[] count_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)operation.Count));
            operation.Request[m_dataBodyOffset + 3] = count_byte[0];
            operation.Request[m_dataBodyOffset + 4] = count_byte[1];
        }
        protected override void EncodeWrite(byte[] request, string uid, string address, string value)
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
            ParseEntity(address, false, out ushort address_int16, out request[m_dataBodyOffset], out byte entity_type);

            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(address_int16)));
            request[m_dataBodyOffset + 1] = address_byte[0];
            request[m_dataBodyOffset + 2] = address_byte[1];
            //value
            UInt16 value_int = (UInt16)Convert.ToInt32(value);
            if (entity_type == '0' && value_int == 1)
            {
                request[m_dataBodyOffset + 3] = 0xFF;
                request[m_dataBodyOffset + 4] = 0x00;
            }
            else
            {
                byte[] val_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)value_int));
                request[m_dataBodyOffset + 3] = val_byte[0];
                request[m_dataBodyOffset + 4] = val_byte[1];
            }
        }
        protected override async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            byte[] response = new byte[m_bufSize];
            if (m_socket != null && m_socket.Connected)
            {
                lock (m_socketLock)
                {
                    try
                    {
                        m_socket.Send(request, reqLen, SocketFlags.None);
                        response = ReadResponse();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something wrong with the socket, disposing...");
                        Console.WriteLine(e.Message);
                        m_socket.Disconnect(false);
                        m_socket.Dispose();
                        m_socket = null;
                    }
                }
                return response;
            }
            else
            {
                Console.WriteLine("Connection lost, reconnecting...");
                await ConnectSlave();
                return null;
            }
        }
        private byte[] ReadResponse()
        {
            byte[] response = new byte[m_bufSize];
            int header_len = 0;
            int data_len = 0;

            int h_l = m_socket.Receive(response, 0, m_dataBodyOffset, SocketFlags.None);
            header_len += h_l;
            while (header_len < m_dataBodyOffset)
            {
                h_l = m_socket.Receive(response, header_len, m_dataBodyOffset - header_len, SocketFlags.None);
                header_len += h_l;
            }

            int byte_counts = IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(response, 4)) - 1;
            int d_l = m_socket.Receive(response, m_dataBodyOffset, byte_counts, SocketFlags.None);
            data_len += d_l;
            while (data_len < byte_counts)
            {
                d_l = m_socket.Receive(response, m_dataBodyOffset + data_len, byte_counts - data_len, SocketFlags.None);
                data_len += d_l;
            }

            return response;
        }
        #endregion
    }

    /// <summary>
    /// This class is Modbus RTU session.
    /// </summary>
    class ModbusRTUSlaveSession : ModbusSlaveSession
    {
        #region Constructors
        public ModbusRTUSlaveSession(ModbusSlaveConfig conf, HandleResultDelegate resultHandler)
            : base(conf, resultHandler)
        {
        }
        #endregion

        #region Protected Properties
        protected override int m_reqSize { get { return 8; } }
        protected override int m_dataBodyOffset { get { return 1; } }
        #endregion

        #region Private Fields
        private const int m_numOfBits = 8;
        private object m_serialPortLock = new object();
        private SerialPort m_serialPort = null;
        #endregion

        #region Public Methods
        public override void ReleaseSession()
        {
            ReleaseOperations();
            if (m_serialPort != null)
            {
                m_serialPort.Close();
                m_serialPort.Dispose();
                m_serialPort = null;
            }
        }
        #endregion

        #region Private Methods
        protected override async Task ConnectSlave()
        {
            if (config.SlaveConnection.Substring(0, 3) == "COM" || config.SlaveConnection.Substring(0, 4) == "ttyS")
            {
                try
                {
                    m_serialPort = new SerialPort(config.SlaveConnection, (int)config.BaudRate, config.Parity, (int)config.DataBits, config.StopBits);
                    m_serialPort.Open();
                    m_serialPort.Handshake = Handshake.None;
                    //m_serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    await Task.Delay(0);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connect Slave failed");
                    Console.WriteLine(e.Message);
                    m_serialPort = null;
                }
            }
        }
        protected override void EncodeRead(ReadOperation operation)
        {
            //uid
            operation.Request[0] = operation.UnitId;

            //Body
            //function code
            operation.Request[m_dataBodyOffset] = operation.FunctionCode;
            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(operation.Address)));
            operation.Request[m_dataBodyOffset + 1] = address_byte[0];
            operation.Request[m_dataBodyOffset + 2] = address_byte[1];
            //count
            byte[] count_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)operation.Count));
            operation.Request[m_dataBodyOffset + 3] = count_byte[0];
            operation.Request[m_dataBodyOffset + 4] = count_byte[1];

            if (GetCRC(operation.Request, 6, out UInt16 crc))
            {
                byte[] crc_byte = BitConverter.GetBytes(crc);
                operation.Request[m_dataBodyOffset + 5] = crc_byte[0];
                operation.Request[m_dataBodyOffset + 6] = crc_byte[1];
            }
        }
        protected override void EncodeWrite(byte[] request, string uid, string address, string value)
        {
            //uid
            request[0] = Convert.ToByte(uid);

            //Body
            //function code
            ParseEntity(address, false, out ushort address_int16, out request[m_dataBodyOffset], out byte entity_type);

            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(address_int16)));
            request[m_dataBodyOffset + 1] = address_byte[0];
            request[m_dataBodyOffset + 2] = address_byte[1];
            //value
            UInt16 value_int = (UInt16)Convert.ToInt32(value);
            if (entity_type == '0' && value_int == 1)
            {
                request[m_dataBodyOffset + 3] = 0xFF;
                request[m_dataBodyOffset + 4] = 0x00;
            }
            else
            {
                byte[] val_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)value_int));
                request[m_dataBodyOffset + 3] = val_byte[0];
                request[m_dataBodyOffset + 4] = val_byte[1];
            }
            if (GetCRC(request, 6, out UInt16 crc))
            {
                byte[] crc_byte = BitConverter.GetBytes(crc);
                request[m_dataBodyOffset + 5] = crc_byte[0];
                request[m_dataBodyOffset + 6] = crc_byte[1];
            }
        }
        protected override async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            //double slient_interval = 1000 * 5 * ((double)1 / (double)config.BaudRate);
            int silent = 100;
            byte[] response = null;
            if (m_serialPort != null && m_serialPort.IsOpen)
            {
                lock (m_serialPortLock)
                {
                    try
                    {
                        m_serialPort.DiscardInBuffer();
                        m_serialPort.DiscardOutBuffer();
                        Task.Delay(silent).Wait();
                        m_serialPort.Write(request, 0, reqLen);
                        response = ReadResponse();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something wrong with the connection, disposing...");
                        Console.WriteLine(e.Message);
                        m_serialPort.Close();
                        m_serialPort.Dispose();
                        m_serialPort = null;
                    }
                }
                return response;
            }
            else
            {
                Console.WriteLine("Connection lost, reconnecting...");
                await ConnectSlave();
                return null;
            }
        }
        private byte[] ReadResponse()
        {
            byte[] response = new byte[m_bufSize];
            int header_len = 0;
            int data_len = 0;

            int h_l = m_serialPort.Read(response, 0, 3);
            header_len += h_l;
            while (header_len < 3)
            {
                h_l = m_serialPort.Read(response, header_len, 3 - header_len);
                header_len += h_l;
            }

            int byte_counts = response[2] + 2;
            int d_l = m_serialPort.Read(response, 3, byte_counts);
            data_len += d_l;
            while (data_len < byte_counts)
            {
                d_l = m_serialPort.Read(response, 3 + data_len, byte_counts - data_len);
                data_len += d_l;
            }

            return response;
        }
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = m_serialPort.ReadExisting();
        }
        private bool GetCRC(byte[] message, int length, out UInt16 res)
        {
            UInt16 crcFull = 0xFFFF;
            res = 0;

            if (message == null || length <= 0)
                return false;

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

    /// <summary>
    /// This class contains the configuration for a Modbus session.
    /// </summary>
    class ModbusSlaveConfig
    {
        public string SlaveConnection { get; set; }
        public string HwId { get; set; }
        public uint BaudRate { get; set; }
        public StopBits StopBits { get; set; }
        public byte DataBits { get; set; }
        public Parity Parity { get; set; }
        //public byte FlowControl { get; set; }
        public Dictionary<string, ReadOperation> Operations = null;
        public ModbusConstants.ConnectionType GetConnectionType()
        {
            if (IPAddress.TryParse(SlaveConnection, out IPAddress address))
                return ModbusConstants.ConnectionType.ModbusTCP;
            else if (SlaveConnection.Substring(0, 3) == "COM" || SlaveConnection.Substring(0, 4) == "ttyS")
                return ModbusConstants.ConnectionType.ModbusRTU;
            //TODO: ModbusRTU ModbusASCII
            return ModbusConstants.ConnectionType.Unknown;
        }

    }

    /// <summary>
    /// This class contains the configuration for a single Modbus read request.
    /// </summary>
    class ReadOperation
    {
        public byte[] Request;
        public byte[] Response;
        public int RequestLen;
        public byte EntityType { get; set; }
        public string OutFormat { get; set; }
        public int PollingInterval { get; set; }
        public byte UnitId { get; set; }
        public byte FunctionCode { get; set; }
        public string StartAddress { get; set; }
        public UInt16 Address { get; set; }
        public UInt16 Count { get; set; }
        public string DisplayName { get; set; }
    }

    static class ModbusConstants
    {
        public enum EntityType
        {
            CoilStatus = '0',
            InputStatus = '1',
            InputRegister = '3',
            HoldingRegister = '4'
        }
        public enum ConnectionType
        {
            Unknown = 0,
            ModbusTCP = 1,
            ModbusRTU = 2,
            ModbusASCII = 3
        };
        public enum FunctionCodeType
        {
            ReadCoils = 1,
            ReadInputs = 2,
            ReadHoldingRegisters = 3,
            ReadInputRegisters = 4,
            WriteCoil = 5,
            WriteHoldingRegister = 6
        };
    }

    class ModbusOutMessage
    {
        public string DisplayName { get; set; }
        public string HwId { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
        public string SourceTimestamp { get; set; }
    }

    class ModbusInMessage
    {
        public string HwId { get; set; }
        public string UId { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
    }

    class ModuleConfig
    {
        public Dictionary<string, ModbusSlaveConfig> SlaveConfigs;
        public ModuleConfig(Dictionary<string, ModbusSlaveConfig> slaves)
        {
            SlaveConfigs = slaves;
        }
    }

    class ModbusPushInterval
    {
        public ModbusPushInterval(int interval)
        {
            PublishInterval = interval;
        }
        public int PublishInterval { get; set; }
    }
}
