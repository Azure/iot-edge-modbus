namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// This class contains the handle for this module. In this case, it is a list of active Modbus sessions.
    /// </summary>
    public class SessionsHandle : ISessionsHandle
    {
        public async Task<SessionsHandle> CreateHandleFromConfiguration(ModuleConfig config)
        {
            SessionsHandle sessionsHandle = null;
            foreach (var configPair in config.SlaveConfigs)
            {
                ModbusSlaveConfig slaveConfig = configPair.Value;

                switch (slaveConfig.GetConnectionType())
                {
                    case ConnectionType.ModbusTCP:
                        {
                            sessionsHandle = sessionsHandle ?? new SessionsHandle();

                            ModbusSlaveSession slave = new ModbusTCPSlaveSession(slaveConfig);
                            await slave.InitSessionAsync().ConfigureAwait(false);
                            sessionsHandle.modbusSessionList.Add(slave);
                            break;
                        }
                    case ConnectionType.ModbusRTU:
                        {
                            sessionsHandle = sessionsHandle ?? new SessionsHandle();

                            ModbusSlaveSession slave = new ModbusRTUSlaveSession(slaveConfig);
                            await slave.InitSessionAsync().ConfigureAwait(false);
                            sessionsHandle.modbusSessionList.Add(slave);
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

        public readonly List<ModbusSlaveSession> modbusSessionList = new List<ModbusSlaveSession>();

        public async Task ReleaseAsync()
        {
            await Task.WhenAll(this.modbusSessionList.Select(s => s.ReleaseSessionAsync())).ConfigureAwait(false);
            
            this.modbusSessionList.Clear();
        }

        public async Task<List<ModbusOutContent>> CollectAndResetOutMessageFromSessionsAsync()
        {
            var contents = new List<ModbusOutContent>();

            foreach (ModbusSlaveSession session in this.modbusSessionList)
            {
                var message = session.GetOutMessage();
                if (message != null)
                {
                    contents.Add(message);
                    await session.ClearOutMessageAsync().ConfigureAwait(false);
                }
            }
            return contents;
        }
    }
}