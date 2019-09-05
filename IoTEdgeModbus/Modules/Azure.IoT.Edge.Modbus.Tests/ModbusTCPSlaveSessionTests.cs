using AzureIoTEdgeModbus.Slave;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Azure.IoT.Edge.Modbus.Tests
{
    [TestClass]
    public class ModbusTCPSlaveSessionTests
    {
        private ModuleConfig config;

        ExposedModbusTCPSlaveSession slaveSession;

        [TestInitialize]
        public void Setup()
        {
            config = ConfigBuilder.CreateTCPOnlyConfig();
            slaveSession = new ExposedModbusTCPSlaveSession(config.SlaveConfigs.Values.First());
        }

        [TestMethod]
        public void CanProcessResponseForIntReadOperation()
        {
            //Arrange
            var intReplies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/IntReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.ProcessResponse(config.SlaveConfigs.Values.First(), intReplies);

            //Assert
            Assert.AreEqual<string>(res[0].Value, "52429", "Unexpected response value");
            
            //In this test we test processing of other fields as well
            Assert.AreEqual<int>(res.Count(), 3, "Unexpected count of out results");
            Assert.AreEqual<string>(res[1].Value, "0", "Unexpected response value");
            Assert.AreEqual<string>(res[0].Address, config.SlaveConfigs.Values.First().Operations.ElementAt(1).Value.StartAddress, "Unexpected cell address");
            Assert.AreEqual<string>(res[0].DisplayName, config.SlaveConfigs.Values.First().Operations.ElementAt(1).Value.DisplayName, "Unexpected result display name");
        }

        [TestMethod]
        public void CanProcessResponseForFloatReadOperation()
        {
            //Arrange
            var floatReplies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/FloatReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.ProcessResponse(config.SlaveConfigs.Values.First(), floatReplies);

            //Assert
            Assert.AreEqual<string>(res[0].Value, "10.750", "Unexpected response value");
        }


        [TestMethod]
        public void CanProcessResponseForInt32ReadOperation()
        {
            //Arrange
            var int32Replies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/Int32ReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.ProcessResponse(config.SlaveConfigs.Values.First(), int32Replies);

            //Assert 
            Assert.AreEqual<string>(res[0].Value, Int32.MaxValue.ToString(), "Unexpected response value");
        }

        [TestMethod]
        public void CanProcessResponseForInputReadOperation()
        {
            //Arrange
            var inputReplies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/InputReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.ProcessResponse(config.SlaveConfigs.Values.First(), inputReplies);

            //Assert
            Assert.AreEqual<int>(res.Count(), 2, "Unexpected count of out results");
            Assert.AreEqual<string>(res[0].Value, "0", "Unexpected response value");
        }

        [TestMethod]
        public async Task CanSetCountForFloatReadOperation()
        {
            //Act
            await slaveSession.InitSession();
            var invalidOps = slaveSession.config.Operations.Where(o => o.Value.ValueType != ModbusValueType.Basic && o.Value.Count != 2).Any();

            //Assert
            Assert.IsTrue(!invalidOps, "Count for IsFloat operation was not set correctly");
        }
    }
}
