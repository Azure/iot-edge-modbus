using AzureIoTEdgeModbus.Slave;
using System;
using System.Collections.Generic;
using System.Text;
using AzureIoTEdgeModbus.Slave.Data;

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
							{ "Op01", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400001", Count = 0, DisplayName= "Voltage", CorrelationId = "MessageType1", DataType= ModbusDataType.Float } },
							{ "Op02", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400002", Count = 3, DisplayName= "Current", CorrelationId = "MessageType2" } },
							{ "Op03", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400003", Count = 3, DisplayName= "Counter", CorrelationId = "MessageType3",DataType= ModbusDataType.Int32 } },
							{ "Op04", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "10001", Count = 2, DisplayName= "OnOff", CorrelationId = "MessageType4", DataType= ModbusDataType.Int32} }
						}
					}
				}
			};
			validModuleConfig.SlaveConfigs = config;
			return validModuleConfig;
		}
	}
}
