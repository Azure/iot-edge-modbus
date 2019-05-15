namespace Modbus.Slaves
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using tempSerialPort;
    using System.IO.Ports;
    using System.Runtime.InteropServices;

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
        public static async Task<ModuleHandle> CreateHandleFromConfiguration(ModuleConfig config)
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

                            ModbusSlaveSession slave = new ModbusTCPSlaveSession(slaveConfig);
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

                            ModbusSlaveSession slave = new ModbusRTUSlaveSession(slaveConfig);
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
        public List<object> CollectAndResetOutMessageFromSessions()
        {
            List<object> obj_list = new List<object>();

            foreach (ModbusSlaveSession session in ModbusSessionList)
            {
                var obj = session.GetOutMessage();
                if (obj != null)
                {
                    obj_list.Add(obj);
                    session.ClearOutMessage();
                }
            }
            return obj_list;
        }

        public List<object> CollectAndResetOutMessageFromSessionsV1()
        {
            List<object> obj_list = new List<object>();

            foreach (ModbusSlaveSession session in ModbusSessionList)
            {
                var obj = session.GetOutMessage();
                if (obj != null)
                {
                    var content = (obj as ModbusOutContent);

                    string hwId = content.HwId;

                    foreach (var data in content.Data)
                    {
                        var sourceTimestamp = data.SourceTimestamp;

                        foreach (var value in data.Values)
                        {
                            obj_list.Add(new ModbusOutMessageV1
                            {
                                HwId = hwId,
                                SourceTimestamp = sourceTimestamp,
                                Address = value.Address,
                                DisplayName = value.DisplayName,
                                Value = value.Value,
                            });
                        }
                    }

                    session.ClearOutMessage();
                }
            }
            
            return obj_list;
        }
    }

    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    abstract class ModbusSlaveSession
    {
        public ModbusSlaveConfig config;
        protected object OutMessage = null;
        protected const int m_bufSize = 512;
        protected SemaphoreSlim m_semaphore_collection = new SemaphoreSlim(1, 1);
        protected SemaphoreSlim m_semaphore_connection = new SemaphoreSlim(1, 1);
        protected bool m_run = false;
        protected List<Task> m_taskList = new List<Task>();
        protected virtual int m_reqSize { get; }
        protected virtual int m_dataBodyOffset { get; }
        protected virtual int m_silent { get; }

        #region Constructors
        public ModbusSlaveSession(ModbusSlaveConfig conf)
        {
            config = conf;
        }
        #endregion

        #region Public Methods
        public abstract void ReleaseSession();
        public async Task InitSession()
        {
            await ConnectSlave();

            foreach (var op_pair in config.Operations)
            {
                ReadOperation x = op_pair.Value;
                
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
        public object GetOutMessage()
        {
            return OutMessage;
        }
        public void ClearOutMessage()
        {
            m_semaphore_collection.Wait();

            OutMessage = null;

            m_semaphore_collection.Release();
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
                    else if (x.Request[m_dataBodyOffset] + ModbusConstants.ModbusExceptionCode == x.Response[m_dataBodyOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {x.Response[m_dataBodyOffset + 1]}");
                    }
                }
                await Task.Delay(x.PollingInterval - m_silent);
            }
        }
        protected void ProcessResponse(ModbusSlaveConfig config, ReadOperation x)
        {
            int count = 0;
            int step_size = 0;
            int start_digit = 0;
            List<ModbusOutValue> value_list = new List<ModbusOutValue>();
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

                ModbusOutValue value = new ModbusOutValue()
                { DisplayName = x.DisplayName, Address = cell, Value = val };
                value_list.Add(value);
            }

            if (value_list.Count > 0)
                PrepareOutMessage(config.HwId, x.CorrelationId, value_list);
        }
        protected void PrepareOutMessage(string HwId, string CorrelationId, List<ModbusOutValue> ValueList)
        {
            m_semaphore_collection.Wait();
            ModbusOutContent content = null;
            if (OutMessage == null)
            {
                content = new ModbusOutContent
                {
                    HwId = HwId,
                    Data = new List<ModbusOutData>()
                };
                OutMessage = content;
            }
            else
            {
                content = (ModbusOutContent)OutMessage;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ModbusOutData data = null;
            foreach(var d in content.Data)
            {
                if (d.CorrelationId == CorrelationId && d.SourceTimestamp == timestamp)
                {
                    data = d;
                    break;
                }
            }
            if(data == null)
            {
                data = new ModbusOutData
                {
                    CorrelationId = CorrelationId,
                    SourceTimestamp = timestamp,
                    Values = new List<ModbusOutValue>()
                };
                content.Data.Add(data);
            }

            data.Values.AddRange(ValueList);

            m_semaphore_collection.Release();

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
        public ModbusTCPSlaveSession(ModbusSlaveConfig conf)
            : base(conf)
        {
        }
        #endregion

        #region Protected Properties
        protected override int m_reqSize { get { return 12; } }
        protected override int m_dataBodyOffset { get { return 7; } }
        protected override int m_silent { get { return 0; } }
        #endregion

        #region Private Fields
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
                    m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = 100
                    };
                    await m_socket.ConnectAsync(m_address, config.TcpPort.Value);
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
            ModuleConfig.ParseEntity(address, false, out ushort address_int16, out request[m_dataBodyOffset], out byte entity_type);

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
            byte[] response = null;
            byte[] garbage = new byte[m_bufSize];

            m_semaphore_connection.Wait();
            
            if (m_socket != null && m_socket.Connected)
            {
                try
                {
                    // clear receive buffer
                    while (m_socket.Available > 0)
                    {
                        int rec = m_socket.Receive(garbage, m_bufSize, SocketFlags.None);
                        Console.WriteLine("Dumping socket receive buffer...");
                        string data = "";
                        int cnt = 0;
                        while(cnt < rec)
                        {
                            data += garbage[cnt].ToString();
                            cnt++;
                        }
                        Console.WriteLine(data);
                    }

                    // send request
                    m_socket.Send(request, reqLen, SocketFlags.None);

                    // read response
                    response = ReadResponse();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Something wrong with the socket, disposing...");
                    Console.WriteLine(e.Message);
                    m_socket.Disconnect(false);
                    m_socket.Dispose();
                    m_socket = null;
                    Console.WriteLine("Connection lost, reconnecting...");
                    await ConnectSlave();
                }
            }
            else
            {
                Console.WriteLine("Connection lost, reconnecting...");
                await ConnectSlave();
            }

            m_semaphore_connection.Release();

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
            bool error = false;

            while(m_socket.Available <= 0 && retry < config.RetryCount)
            {
                retry++;
                Task.Delay(config.RetryInterval.Value).Wait();
            }
            
            while (header_len < m_dataBodyOffset && retry < config.RetryCount)
            {
                if (m_socket.Available > 0)
                {
                    h_l = m_socket.Receive(response, header_len, m_dataBodyOffset - header_len, SocketFlags.None);
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

            while (data_len < byte_counts && retry < config.RetryCount)
            {
                if (m_socket.Available > 0)
                {
                    d_l = m_socket.Receive(response, m_dataBodyOffset + data_len, byte_counts - data_len, SocketFlags.None);
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

            if (retry >= config.RetryCount || error)
            {
                response = null;
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
        public ModbusRTUSlaveSession(ModbusSlaveConfig conf)
            : base(conf)
        {
        }
        #endregion

        #region Protected Properties
        protected override int m_reqSize { get { return 8; } }
        protected override int m_dataBodyOffset { get { return 1; } }
        protected override int m_silent { get { return 100; } }
        #endregion

        #region Private Fields
        private const int m_numOfBits = 8;
        private ISerialDevice m_serialPort = null;
        #endregion

        #region Public Methods
        public override void ReleaseSession()
        {
            ReleaseOperations();
            if (m_serialPort != null)
            {
                m_serialPort.Dispose();
                m_serialPort = null;
            }
        }
        #endregion

        #region Private Methods
        protected override async Task ConnectSlave()
        {
            if (config.SlaveConnection.Substring(0, 3) == "COM" || config.SlaveConnection.Substring(0, 8) == "/dev/tty")
            {
                try
                {
                    Console.WriteLine($"Opening...{config.SlaveConnection}");

                    m_serialPort = SerialDeviceFactory.CreateSerialDevice(config.SlaveConnection, (int)config.BaudRate.Value, config.Parity.Value, (int)config.DataBits.Value, config.StopBits.Value);
                    
                    m_serialPort.Open();
                    //m_serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    await Task.Delay(2000); //Wait target to be ready to write the modbus package
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
            ModuleConfig.ParseEntity(address, false, out ushort address_int16, out request[m_dataBodyOffset], out byte entity_type);

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
            byte[] response = null;

            m_semaphore_connection.Wait();

            if (m_serialPort != null && m_serialPort.IsOpen())
            {
                try
                {
                    m_serialPort.DiscardInBuffer();
                    m_serialPort.DiscardOutBuffer();
                    Task.Delay(m_silent).Wait();
                    m_serialPort.Write(request, 0, reqLen);
                    response = ReadResponse();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Something wrong with the connection, disposing...");
                    Console.WriteLine(e.Message);
                    m_serialPort.Dispose();
                    m_serialPort = null;
                    Console.WriteLine("Connection lost, reconnecting...");
                    await ConnectSlave();
                }
            }
            else
            {
                Console.WriteLine("Connection lost, reconnecting...");
                await ConnectSlave();
            }

            m_semaphore_connection.Release();

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
            
            while (header_len < 3 && retry < config.RetryCount)
            {
                h_l = m_serialPort.Read(response, header_len, 3 - header_len);
                if (h_l > 0)
                {
                    header_len += h_l;
                }
                else
                {
                    retry++;
                    Task.Delay(config.RetryInterval.Value).Wait();
                }
            }

            int byte_counts = response[1] >= ModbusConstants.ModbusExceptionCode ? 2 : response[2] + 2;
            while (data_len < byte_counts && retry < config.RetryCount)
            {
                d_l = m_serialPort.Read(response, 3 + data_len, byte_counts - data_len);
                if (d_l > 0)
                {
                    data_len += d_l;
                }
                else
                {
                    retry++;
                    Task.Delay(config.RetryInterval.Value).Wait();
                }
            }

            if (retry >= config.RetryCount)
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
    /// The base data for a Modbus session used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    class BaseModbusSlaveConfig
    {
        public string SlaveConnection { get; set; }
        public int? RetryCount { get; set; }
        public int? RetryInterval { get; set; }
        public int? TcpPort { get; set; }
        public string HwId { get; set; }
        public uint? BaudRate { get; set; }
        public StopBits? StopBits { get; set; }
        public byte? DataBits { get; set; }
        public Parity? Parity { get; set; }
        //public byte FlowControl { get; set; }
        public Dictionary<string, BaseReadOperation> Operations = null;
    }

    /// <summary>
    /// This class contains the configuration for a Modbus session.
    /// </summary>
    class ModbusSlaveConfig : BaseModbusSlaveConfig
    {
        public new Dictionary<string, ReadOperation> Operations = null;

        public ModbusConstants.ConnectionType GetConnectionType()
        {
            if (IPAddress.TryParse(SlaveConnection, out IPAddress address))
                return ModbusConstants.ConnectionType.ModbusTCP;
            else if (SlaveConnection.Contains("COM") || SlaveConnection.Contains("/tty"))
                return ModbusConstants.ConnectionType.ModbusRTU;
            //TODO: ModbusRTU ModbusASCII
            return ModbusConstants.ConnectionType.Unknown;
        }

        public BaseModbusSlaveConfig AsBase()
        {
          // Unfortunately we need to create new objects since simple polymorphism will still keep the same fields of the child classes.
          BaseModbusSlaveConfig baseConfig = new BaseModbusSlaveConfig
          {
              SlaveConnection = this.SlaveConnection,
              RetryCount = this.RetryCount,
              RetryInterval = this.RetryInterval,
              TcpPort = this.TcpPort,
              HwId = this.HwId,
              BaudRate = this.BaudRate,
              StopBits = this.StopBits,
              DataBits = this.DataBits,
              Parity = this.Parity
          };

          baseConfig.Operations = this.Operations.ToDictionary(
              pair => pair.Key,
              pair => new BaseReadOperation
              {
                  PollingInterval = pair.Value.PollingInterval,
                  UnitId = pair.Value.UnitId,
                  StartAddress = pair.Value.StartAddress,
                  Count = pair.Value.Count,
                  DisplayName = pair.Value.DisplayName,
                  CorrelationId = pair.Value.CorrelationId,
              });


          return baseConfig;
        }
    }

    /// <summary>
    /// The base data for a read operation used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    class BaseReadOperation
    {
        public int PollingInterval { get; set; }
        public byte UnitId { get; set; }
        public string StartAddress { get; set; }
        public UInt16 Count { get; set; }
        public string DisplayName { get; set; }
        public string CorrelationId { get; set; }
    }

    /// <summary>
    /// This class contains the configuration for a single Modbus read request.
    /// </summary>
    class ReadOperation : BaseReadOperation
    {
        public byte[] Request;
        public byte[] Response;
        public int RequestLen;
        public byte EntityType { get; set; }
        public string OutFormat { get; set; }
        public byte FunctionCode { get; set; }
        public UInt16 Address { get; set; }
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
        public static int DefaultTcpPort = 502;
        public static int DefaultRetryCount = 10;
        public static int DefaultRetryInterval = 50;
        public static string DefaultCorrelationId = "DefaultCorrelationId";
        public static int ModbusExceptionCode = 0x80;
    }
    
    class ModbusOutContent
    {
        public string HwId { get; set; }
        public List<ModbusOutData> Data { get; set; }
    }

    class ModbusOutData
    {
        public string CorrelationId { get; set; }
        public string SourceTimestamp { get; set; }
        public List<ModbusOutValue> Values { get; set; }
    }
    class ModbusOutValue
    {
        public string DisplayName { get; set; }
        //public string OpName { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
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
        public string IoTCentralConnectionString;
        public Dictionary<string, ModbusSlaveConfig> SlaveConfigs;
        public ModuleConfig(Dictionary<string, ModbusSlaveConfig> slaves)
        {
            SlaveConfigs = slaves;
        }
        public void Validate()
        {
            List<string> invalidConfigs = new List<string>();
            foreach (var config_pair in SlaveConfigs)
            {
                ModbusSlaveConfig slaveConfig = config_pair.Value;
                if(slaveConfig == null)
                {
                    Console.WriteLine($"{config_pair.Key} is null, remove from dictionary...");
                    invalidConfigs.Add(config_pair.Key);
                    continue;
                }
                if (slaveConfig.TcpPort == null || slaveConfig.TcpPort <= 0)
                {
                    Console.WriteLine($"Invalid TcpPort: {slaveConfig.TcpPort}, set to DefaultTcpPort: {ModbusConstants.DefaultTcpPort}");
                    slaveConfig.TcpPort = ModbusConstants.DefaultTcpPort;
                }
                if (slaveConfig.RetryCount == null || slaveConfig.RetryCount <= 0)
                {
                    Console.WriteLine($"Invalid RetryCount: {slaveConfig.RetryCount}, set to DefaultRetryCount: {ModbusConstants.DefaultRetryCount}");
                    slaveConfig.RetryCount = ModbusConstants.DefaultRetryCount;
                }
                if (slaveConfig.RetryInterval == null || slaveConfig.RetryInterval <= 0)
                {
                    Console.WriteLine($"Invalid RetryInterval: {slaveConfig.RetryInterval}, set to DefaultRetryInterval: {ModbusConstants.DefaultRetryInterval}");
                    slaveConfig.RetryInterval = ModbusConstants.DefaultRetryInterval;
                }
                List<string> invalidOperations = new List<string>();
                foreach (var operation_pair in slaveConfig.Operations)
                {
                    ReadOperation operation = operation_pair.Value;
                    if(operation == null)
                    {
                        Console.WriteLine($"{operation_pair.Key} is null, remove from dictionary...");
                        invalidOperations.Add(operation_pair.Key);
                        continue;
                    }
                    if(operation.StartAddress.Length < 5)
                    {
                        Console.WriteLine($"{operation_pair.Key} has invalid StartAddress {operation.StartAddress}, remove from dictionary...");
                        invalidOperations.Add(operation_pair.Key);
                        continue;
                    }
                    ParseEntity(operation.StartAddress, true, out ushort address_int16, out byte function_code, out byte entity_type);

                    if (operation.Count <= 0)
                    {
                        Console.WriteLine($"{operation_pair.Key} has invalid Count {operation.Count}, remove from dictionary...");
                        invalidOperations.Add(operation_pair.Key);
                        continue;
                    }
                    if (operation.Count > 127 && ((char)entity_type == (char)ModbusConstants.EntityType.HoldingRegister || (char)entity_type == (char)ModbusConstants.EntityType.InputRegister))
                    {
                        Console.WriteLine($"{operation_pair.Key} has invalid Count, must be 1~127, {operation.Count}, remove from dictionary...");
                        invalidOperations.Add(operation_pair.Key);
                        continue;
                    }
                    if(operation.CorrelationId == "" || operation.CorrelationId == null)
                    {
                        Console.WriteLine($"Empty CorrelationId: {operation.CorrelationId}, set to DefaultCorrelationId: {ModbusConstants.DefaultCorrelationId}");
                        operation.CorrelationId = ModbusConstants.DefaultCorrelationId;
                    }
                    operation.EntityType = entity_type;
                    operation.Address = address_int16;
                    operation.FunctionCode = function_code;
                    //output format
                    if (operation.StartAddress.Length == 5)
                        operation.OutFormat = "{0}{1:0000}";
                    else if (operation.StartAddress.Length == 6)
                        operation.OutFormat = "{0}{1:00000}";
                }
                foreach(var in_op in invalidOperations)
                {
                    slaveConfig.Operations.Remove(in_op);
                }
            }
            foreach(var in_slave in invalidConfigs)
            {
                SlaveConfigs.Remove(in_slave);
            }
        }
        public static bool ParseEntity(string startAddress, bool isRead, out ushort outAddress, out byte functionCode, out byte entityType)
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
    }

    class ModbusPushInterval
    {
        public ModbusPushInterval(int interval)
        {
            PublishInterval = interval;
        }
        public int PublishInterval { get; set; }
    }

    class ModbusVersion
    {
        public ModbusVersion(string version)
        {
            Version = version;
        }
        public string Version { get; set; }
    }

    class ModbusOutMessageV1
    {
        public string DisplayName { get; set; }
        public string HwId { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
        public string SourceTimestamp { get; set; }
    }

    class ModbusOutMessage
    {
        public string PublishTimestamp { get; set; }
        public List<object> Content { get; set; }
    }

    /// <summary>
    /// This class creates a container to easily serialize the configuration so that the reported
    /// properties can be updated.
    /// </summary>
    class ModuleTwinProperties
    {
        public ModuleTwinProperties(int? publishInterval, ModuleConfig moduleConfig)
        {
          PublishInterval = publishInterval;
          SlaveConfigs = moduleConfig?.SlaveConfigs.ToDictionary(pair => pair.Key, pair => pair.Value.AsBase());
        }

        public int? PublishInterval { get; set; }
        public Dictionary<string, BaseModbusSlaveConfig> SlaveConfigs { get; set; }
    }
}
