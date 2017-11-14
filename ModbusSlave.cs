

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
        public List<ModbusSlaveSession> ModbusSessionList = new List<ModbusSlaveSession>();
        public ModbusSlaveSession GetSlaveSession(string hwid)
        {
            return ModbusSessionList.Find(x => x.config.HwId.ToUpper() == hwid.ToUpper());
        }
    }
    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    abstract class ModbusSlaveSession
    {
        public ModbusSlaveConfig config;
        public abstract Task ProcessOperations(List<ModbusOutMessage> result);
        public abstract Task WriteCB(string uid, string address, string value);
        public abstract Task InitSession();
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
        private const int bufsz = 512;
        private Socket socket = null;
        private IPAddress address = null;
        private const int TCPOffset = 7;
        private object socketLock = new object();
        #endregion
        #region Public Methods
        public override async Task WriteCB(string uid, string address, string value)
        {
            byte[] writeRequest = new byte[bufsz];
            byte[] writeResponse;
            int reqLen = 12;

            EncodeWrite(writeRequest, uid, address, value);
            
            writeResponse = await SendRequest(writeRequest, reqLen);
        }
        public override async Task ProcessOperations(List<ModbusOutMessage> result)
        {
            foreach (var op_pair in config.Operations)
            {
                ReadOperation x = op_pair.Value;
                x.Response = await SendRequest(x.Request, x.RequestLen);

                if (x.Response != null)
                {
                    if (x.Request[TCPOffset] == x.Response[TCPOffset])
                    {
                        ProcessResponse(config, x, result);
                    }
                    else if (x.Request[TCPOffset] + 0x80 == x.Response[TCPOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {x.Response[TCPOffset + 1]}");
                    }
                }
                x.Response = null;
            }
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

                x.RequestLen = 12;
                x.Request = new byte[bufsz];

                EncodeRead(x);
            }
        }
        #endregion
        #region Private Methods
        private async Task ConnectSlave()
        {
            if (IPAddress.TryParse(config.SlaveConnection, out address))
            {
                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(address, 502);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connect Slave failed");
                    Console.WriteLine(e.Message);
                    socket = null;
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
                case '0':
                    {
                        functionCode = (byte)(isRead ? 1 : 5);
                        break;
                    }
                case '1':
                    {
                        if (isRead)
                            functionCode = 2;
                        else
                            return false;
                        break;
                    }
                case '3':
                    {
                        if (isRead)
                            functionCode = 4;
                        else
                            return false;
                        break;
                    }
                case '4':
                    {
                        functionCode = (byte)(isRead ? 3 : 6);
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
            operation.Request[TCPOffset] = operation.FunctionCode;
            //address
            byte[] address_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)(operation.Address)));
            operation.Request[8] = address_byte[0];
            operation.Request[9] = address_byte[1];
            //count
            byte[] count_byte = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16)operation.Count));
            operation.Request[10] = count_byte[0];
            operation.Request[11] = count_byte[1];
        }
        private void EncodeWrite(byte []request, string uid, string address, string value)
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
            ParseEntity(address, false, out ushort address_int16, out request[TCPOffset], out byte entity_type);

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
        private void ProcessResponse(ModbusSlaveConfig config, ReadOperation x, List<ModbusOutMessage> result)
        {
            int count = 0;
            int step_size = 0;
            int start_digit = 0;
            switch (x.Response[TCPOffset])//function code
            {
                case 1:
                case 2:
                    {
                        count = x.Response[TCPOffset + 1] * 8;
                        count = (count > x.Count) ? x.Count : count;
                        step_size = 1;
                        start_digit = x.Response[TCPOffset] - 1;
                        break;
                    }
                case 3:
                case 4:
                    {
                        count = x.Response[TCPOffset + 1];
                        step_size = 2;
                        start_digit = (x.Response[TCPOffset] == 3) ? 4 : 3;
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
                    val = string.Format("{0}", (x.Response[TCPOffset + 2 + (i / 8)] >> (i % 8)) & 0b1);
                }
                else if (step_size == 2)
                {
                    cell = string.Format(x.OutFormat, (char)x.EntityType, x.Address + (i / 2) + 1);
                    val = string.Format("{0,00000}", ((x.Response[TCPOffset + 2 + i]) * 0x100 + x.Response[TCPOffset + 3 + i]));
                }
                res = cell + ": " + val + "\n";
                Console.WriteLine(res);

                ModbusOutMessage message = new ModbusOutMessage()
                { HwId = config.HwId, DisplayName = x.DisplayName, Address = cell, Value = val, SourceTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                result.Add(message);
            }
        }
        private async Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            byte[] response = new byte[bufsz];
            if (socket != null && socket.Connected)
            {
                lock (socketLock)
                {
                    try
                    {
                        socket.Send(request, reqLen, SocketFlags.None);
                        socket.Receive(response, 0, TCPOffset, SocketFlags.None);
                        int remain = IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(response, 4));
                        socket.Receive(response, TCPOffset, remain - 1, SocketFlags.None);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something wrong with the socket, disposing...");
                        Console.WriteLine(e.Message);
                        socket.Disconnect(false);
                        socket.Dispose();
                        socket = null;
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
        public enum ConnectionType
        {
            Unknown = 0,
            ModbusTCP = 1,
            ModbusRTU = 2,
            ModbusASCII = 3
        };
        public string SlaveConnection { get; set; }
        public string HwId { get; set; }
        public int Interval { get; set; }
        /*
        public uint BaudRate { get; set; }
        public byte StopBits { get; set; }
        public byte DataBits { get; set; }
        public byte Parity { get; set; }
        public byte FlowControl { get; set; }
        */
        public Dictionary<string, ReadOperation> Operations = null;
        public ConnectionType GetConnectionType()
        {
            if (IPAddress.TryParse(SlaveConnection, out IPAddress address))
                return ConnectionType.ModbusTCP;
            //TODO: ModbusRTU ModbusASCII
            return ConnectionType.Unknown;
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
}
