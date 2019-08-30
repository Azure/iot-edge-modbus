namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// This class contains the handle for this module. In this case, it is a list of active Modbus sessions.
    /// </summary>
    public class SessionsHandle : ISessionsHandle
    {
        public async Task<SessionsHandle> CreateHandleFromConfiguration(ModuleConfig config)
        {
            SessionsHandle sessionsHandle = null;
            foreach (var config_pair in config.SlaveConfigs)
            {
                ModbusSlaveConfig slaveConfig = config_pair.Value;

                switch (slaveConfig.GetConnectionType())
                {
                    case ConnectionType.ModbusTCP:
                        {
                            if (sessionsHandle == null)
                            {
                                sessionsHandle = new SessionsHandle();
                            }

                            ModbusSlaveSession slave = new ModbusTCPSlaveSession(slaveConfig);
                            await slave.InitSession();
                            sessionsHandle.ModbusSessionList.Add(slave);
                            break;
                        }
                    case ConnectionType.ModbusRTU:
                        {
                            if (sessionsHandle == null)
                            {
                                sessionsHandle = new SessionsHandle();
                            }

                            ModbusSlaveSession slave = new ModbusRTUSlaveSession(slaveConfig);
                            await slave.InitSession();
                            sessionsHandle.ModbusSessionList.Add(slave);
                            break;
                        }
                    case ConnectionType.ModbusASCII:
                        {
                            break;
                        }
                    case ConnectionType.Unknown:
                        {
                            break;
                        }
                }
            }
            return sessionsHandle;
        }

        public List<ModbusSlaveSession> ModbusSessionList = new List<ModbusSlaveSession>();

        public ModbusSlaveSession GetSlaveSession(string hwid)
        {
            return this.ModbusSessionList.Find(x => x.config.HwId.ToUpper() == hwid.ToUpper());
        }
        public void Release()
        {
            foreach (var session in this.ModbusSessionList)
            {
                session.ReleaseSession();
            }
            this.ModbusSessionList.Clear();
        }
        public List<ModbusOutContent> CollectAndResetOutMessageFromSessions()
        {
            var contents = new List<ModbusOutContent>();

            foreach (ModbusSlaveSession session in this.ModbusSessionList)
            {
                ModbusOutContent message = session.GetOutMessage();
                if (message != null)
                {
                    contents.Add(message);
                    session.ClearOutMessage();
                }
            }
            return contents;
        }
        public List<object> CollectAndResetOutMessageFromSessionsV1()
        {
            List<object> obj_list = new List<object>();

            foreach (ModbusSlaveSession session in this.ModbusSessionList)
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
}