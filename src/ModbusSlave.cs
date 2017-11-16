

namespace Modbus.Slaves
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

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
        public abstract Task<List<ModbusOutMessage>> ProcessOperations();
        public abstract Task WriteCB(string uid, string address, string value);
        public abstract Task InitSession();
        public abstract void ReleaseSession();
    }
    /*
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
    /// This class is Modbus TCP session.
    /// </summary>
    class ModbusTCPSlaveSession : ModbusSlaveSession
    {
        #region Constructors
        public ModbusTCPSlaveSession(ModbusSlaveConfig conf)
        {
            config = conf;
        }
        #endregion

        #region Private Properties
        private const int m_reqSize = 12;
        private const int m_bufSize = 512;
        private const int m_tcpPort = 502;
        private const int m_tcpOffset = 7;
        private object m_socketLock = new object();
        private Socket m_socket = null;
        private IPAddress m_address = null;
        #endregion
        #region Public Methods
        public override async Task WriteCB(string uid, string address, string value)
        {
            byte[] writeRequest = new byte[m_bufSize];
            byte[] writeResponse;
            int reqLen = m_reqSize;

            EncodeWrite(writeRequest, uid, address, value);
            writeResponse = await SendRequest(writeRequest, reqLen);
        }
        public override async Task<List<ModbusOutMessage>> ProcessOperations()
        {
            List<ModbusOutMessage> result = new List<ModbusOutMessage>();
            foreach (var op_pair in config.Operations)
            {
                ReadOperation x = op_pair.Value;
                x.Response = await SendRequest(x.Request, x.RequestLen);

                if (x.Response != null)
                {
                    if (x.Request[m_tcpOffset] == x.Response[m_tcpOffset])
                    {
                        var msg = ProcessResponse(config, x);
                        result.AddRange(msg);
                    }
                    else if (x.Request[m_tcpOffset] + 0x80 == x.Response[m_tcpOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {x.Response[m_tcpOffset + 1]}");
                    }
                }
                x.Response = null;
            }
            return result;
        }
        public override async Task InitSession()
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

        public override void ReleaseSession()
        {
            if (m_socket != null)
            {
                m_socket.Disconnect(false);
                m_socket.Dispose();
                m_socket = null;
            }
        }
        #endregion
        #region Private Methods
        private async Task ConnectSlave()
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
        private bool ParseEntity(string startAddress, bool isRead, out ushort outAddress, out byte functionCode, out byte entityType)
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
        private void EncodeRead(ReadOperation operation)
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
            operation.Request[m_tcpOffset] = operation.FunctionCode;
            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(operation.Address)));
            operation.Request[8] = address_byte[0];
            operation.Request[9] = address_byte[1];
            //count
            byte[] count_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)operation.Count));
            operation.Request[10] = count_byte[0];
            operation.Request[11] = count_byte[1];
        }
        private void EncodeWrite(byte[] request, string uid, string address, string value)
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
            ParseEntity(address, false, out ushort address_int16, out request[m_tcpOffset], out byte entity_type);

            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(address_int16)));
            request[8] = address_byte[0];
            request[9] = address_byte[1];
            //value
            UInt16 value_int = (UInt16)Convert.ToInt32(value);
            if (entity_type == '0' && value_int == 1)
            {
                request[10] = 0xFF;
                request[11] = 0x00;
            }
            else
            {
                byte[] val_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)value_int));
                request[10] = val_byte[0];
                request[11] = val_byte[1];
            }
        }
        private List<ModbusOutMessage> ProcessResponse(ModbusSlaveConfig config, ReadOperation x)
        {
            List<ModbusOutMessage> result = new List<ModbusOutMessage>();
            int count = 0;
            int step_size = 0;
            int start_digit = 0;
            switch (x.Response[m_tcpOffset])//function code
            {
                case (byte)ModbusConstants.FunctionCodeType.ReadCoils:
                case (byte)ModbusConstants.FunctionCodeType.ReadInputs:
                    {
                        count = x.Response[m_tcpOffset + 1] * 8;
                        count = (count > x.Count) ? x.Count : count;
                        step_size = 1;
                        start_digit = x.Response[m_tcpOffset] - 1;
                        break;
                    }
                case (byte)ModbusConstants.FunctionCodeType.ReadHoldingRegisters:
                case (byte)ModbusConstants.FunctionCodeType.ReadInputRegisters:
                    {
                        count = x.Response[m_tcpOffset + 1];
                        step_size = 2;
                        start_digit = (x.Response[m_tcpOffset] == 3) ? 4 : 3;
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
                    val = string.Format("{0}", (x.Response[m_tcpOffset + 2 + (i / 8)] >> (i % 8)) & 0b1);
                }
                else if (step_size == 2)
                {
                    cell = string.Format(x.OutFormat, (char)x.EntityType, x.Address + (i / 2) + 1);
                    val = string.Format("{0,00000}", ((x.Response[m_tcpOffset + 2 + i]) * 0x100 + x.Response[m_tcpOffset + 3 + i]));
                }
                res = cell + ": " + val + "\n";
                Console.WriteLine(res);

                ModbusOutMessage message = new ModbusOutMessage()
                { HwId = config.HwId, DisplayName = x.DisplayName, Address = cell, Value = val, SourceTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                result.Add(message);
            }
            return result;
        }
        private async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            byte[] response = new byte[m_bufSize];
            if (m_socket != null && m_socket.Connected)
            {
                lock (m_socketLock)
                {
                    try
                    {
                        m_socket.Send(request, reqLen, SocketFlags.None);
                        m_socket.Receive(response, 0, m_tcpOffset, SocketFlags.None);
                        int remain = IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(response, 4));
                        m_socket.Receive(response, m_tcpOffset, remain - 1, SocketFlags.None);
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
        #endregion
    }
    //TODO
    //class ModbusRTUSlaveSession : ModbusSlaveSession
    //{
    //}

    /// <summary>
    /// This class contains the configuration for a Modbus session.
    /// </summary>
    class ModbusSlaveConfig
    {
        public string SlaveConnection { get; set; }
        public string HwId { get; set; }
        /*
        public uint BaudRate { get; set; }
        public byte StopBits { get; set; }
        public byte DataBits { get; set; }
        public byte Parity { get; set; }
        public byte FlowControl { get; set; }
        */
        public Dictionary<string, ReadOperation> Operations = null;
        public ModbusConstants.ConnectionType GetConnectionType()
        {
            if (IPAddress.TryParse(SlaveConnection, out IPAddress address))
                return ModbusConstants.ConnectionType.ModbusTCP;
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
    class ModbusSendInterval
    {
        public ModbusSendInterval(int interval)
        {
            Interval = interval;
        }
        public int Interval { get; set; }
    }
}
