using AzureIoTEdgeModbus.Slave;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.IoT.Edge.Modbus.Tests
{
    class ConfigBuilder
    {
        public static ModuleConfig CreateTCPOnlyConfig()
        {

            var validModuleConfig = new ModuleConfig
            {
                PublishInterval = 5000
            };

            var config = new Dictionary<string, ModbusSlaveConfig>
            {
                { "Slave1",
                 new ModbusSlaveConfig
                    {
                        TcpPort = 502,
                        SlaveConnection = "127.0.0.1",
                        HwId = "PowerMeter-0a:01:01:01:01:01",
                        RetryCount = 1,
                        RetryInterval = 100,
                        Operations = new Dictionary<string, ReadOperation>()
                        {
                            { "Op01", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400001", Count = 0, DisplayName= "Voltage", CorrelationId = "MessageType1", IsFloat=true } },
                            { "Op02", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400002", Count = 3, DisplayName= "Current", CorrelationId = "MessageType1", IsFloat=false } },
                            { "Op03", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "10001", Count = 2, DisplayName= "OnOff", CorrelationId = "MessageType1", IsFloat=true} }
                        }
                    }
                }
            };
            validModuleConfig.SlaveConfigs = config;
            return validModuleConfig;
        }
    }
}
