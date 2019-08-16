namespace Azure.IoT.Edge.Modbus.Tests
{
    using AzureIoTEdgeModbus.Configuration;
    using AzureIoTEdgeModbus.DeviceTwin;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Wrappers;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class DeviceConfigurationTests
    {
        private ModuleConfig validModuleConfig;
        private ModuleConfig invalidModuleConfig;

        [TestInitialize]
        public void Setup()
        {
            this.validModuleConfig = new ModuleConfig
            {
                PublishInterval = 5000
            };

            this.validModuleConfig.SlaveConfigs = new Dictionary<string, ModbusSlaveConfig>
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
                            { "Op01", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400001", Count = 2, DisplayName= "Voltage", CorrelationId = "MessageType1" } },
                            { "Op02", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400002", Count = 2, DisplayName= "Current", CorrelationId = "MessageType1" } }
                        }
                    }
                }
            };

            this.invalidModuleConfig = new ModuleConfig
            {
                PublishInterval = 0
            };

            this.invalidModuleConfig.SlaveConfigs = new Dictionary<string, ModbusSlaveConfig>
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
                            { "Op01", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400001", Count = 2, DisplayName= "Voltage", CorrelationId = "MessageType1" } },
                            { "Op02", new ReadOperation{ PollingInterval = 1000, UnitId =1, StartAddress = "400002", Count = 2, DisplayName= "Current", CorrelationId = "MessageType1" } }
                        }
                    }
                }
            };
        }

        [TestMethod]
        public async Task CanRetrieveValidDesiredConfigurationFromFile()
        {
            // Arrange
            var configJson = JsonConvert.SerializeObject(this.validModuleConfig);
            var configJsonReader = new StringReader(configJson);

            // Act
            using (var fileConfig = new FileConfiguration<ModuleConfig>(configJsonReader))
            {
                var typedConfig = await fileConfig.GetDeviceConfigurationAsync(new CancellationToken()).ConfigureAwait(false);

                // Assert
                Assert.AreEqual<string>(configJson, JsonConvert.SerializeObject(typedConfig));
            }
        }

        [TestMethod]
        public async Task CanRetrieveValidDesiredConfigurationFromTwin()
        {
            // Arrange
            var configJson = JsonConvert.SerializeObject(this.validModuleConfig);
            var twinProperties = new TwinProperties()
            {
                Desired = new TwinCollection(configJson)
            };

            var moduleClientMock = new Mock<IModuleClient>();
            moduleClientMock.Setup(mc => mc.GetTwinAsync(new CancellationToken())).ReturnsAsync(new Twin(twinProperties));

            // Act
            var deviceTwinConfig = new DeviceTwinConfiguration<ModuleConfig>(null, moduleClientMock.Object);
            var typedConfig = await deviceTwinConfig.GetDeviceConfigurationAsync(new CancellationToken()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual<string>(configJson, JsonConvert.SerializeObject(typedConfig));
        }

        [TestMethod]
        public async Task CanFallbackToSecondaryDesiredFileConfigurationStore()
        {
            // Arrange            
            var validConfigJson = JsonConvert.SerializeObject(this.validModuleConfig);
            var invalidConfigJson = JsonConvert.SerializeObject(this.invalidModuleConfig);

            var configJsonReader = new StringReader(validConfigJson);
            var twinProperties = new TwinProperties()
            {
                Desired = new TwinCollection(invalidConfigJson)
            };

            var moduleClientMock = new Mock<IModuleClient>();
            moduleClientMock.Setup(mc => mc.GetTwinAsync(new CancellationToken())).ReturnsAsync(new Twin(twinProperties));

            // Act
            using (var fileConfig = new FileConfiguration<ModuleConfig>(configJsonReader))
            {
                var deviceTwinConfig = new DeviceTwinConfiguration<ModuleConfig>(fileConfig, moduleClientMock.Object);
                var typedConfig = await deviceTwinConfig.GetDeviceConfigurationAsync(new CancellationToken()).ConfigureAwait(false);

                // Assert
                Assert.AreEqual<string>(validConfigJson, JsonConvert.SerializeObject(typedConfig));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConfigurationErrorsException), "Expected exception was not thrown by the code for the invalid configuration.")]
        public async Task CanValidateDesiredConfigurationFromFile()
        {
            // Arrange
            var configJson = JsonConvert.SerializeObject(this.invalidModuleConfig);
            var twinProperties = new TwinProperties()
            {
                Desired = new TwinCollection(configJson)
            };

            var moduleClientMock = new Mock<IModuleClient>();
            moduleClientMock.Setup(mc => mc.GetTwinAsync(new CancellationToken())).ReturnsAsync(new Twin(twinProperties));

            // Act
            var deviceTwinConfig = new DeviceTwinConfiguration<ModuleConfig>(null, moduleClientMock.Object);
            var typedConfig = await deviceTwinConfig.GetDeviceConfigurationAsync(new CancellationToken()).ConfigureAwait(false);            
        }
    }
}