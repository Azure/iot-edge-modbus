namespace Azure.IoT.Edge.Modbus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Slave.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MockData;
    using Newtonsoft.Json;

    [TestClass]
    public class ModbusTCPSlaveSessionTests
    {
        private ModuleConfig config;

        private ExposedModbusTCPSlaveSession slaveSession;
        private Dictionary<string, ReadOperation> operations;

        [TestInitialize]
        public void Setup()
        {
            config = ConfigBuilder.CreateTCPOnlyConfig();
            slaveSession = new ExposedModbusTCPSlaveSession(config.SlaveConfigs.Values.First());
            operations = config.SlaveConfigs.Values.First().Operations;

        }

        [TestMethod]
        public void CanProcessResponseForIntReadOperation()
        {
            //Arrange
            var intReplies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/IntReadOperationWithResponse.json")));

            //Act
            var operation = config.SlaveConfigs.Values.First().Operations.First().Value;
            var res = slaveSession.DecodeResponse(operation);

            //Assert
            Assert.AreEqual<string>(res[0].Value, "52429", "Unexpected response value");
            
            //In this test we test processing of other fields as well
            Assert.AreEqual<int>(res.Count(), 3, "Unexpected count of out results");
            Assert.AreEqual<string>(res[1].Value, "0", "Unexpected response value");
            Assert.AreEqual<string>(res[0].Address.ToString(), operations.ElementAt(1).Value.StartAddress, "Unexpected cell address");
        }

        [TestMethod]
        public void CanProcessResponseForFloatReadOperation()
        {
            //Arrange
            var floatReplies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/FloatReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.DecodeResponse(operations.Values.First());

            //Assert
            Assert.AreEqual<string>(res[0].Value, "10.750", "Unexpected response value");
        }


        [TestMethod]
        public void CanProcessResponseForInt32ReadOperation()
        {
            //Arrange
            var int32Replies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/Int32ReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.DecodeResponse(operations.First().Value);

            //Assert 
            Assert.AreEqual<string>(res[0].Value, Int32.MaxValue.ToString(), "Unexpected response value");
        }

        [TestMethod]
        public void CanProcessResponseForInputReadOperation()
        {
            //Arrange
            var inputReplies = JsonConvert.DeserializeObject<ReadOperation>((File.ReadAllText("MockData/InputReadOperationWithResponse.json")));

            //Act
            var res = slaveSession.DecodeResponse(operations.First().Value);

            //Assert
            Assert.AreEqual<int>(res.Count(), 2, "Unexpected count of out results");
            Assert.AreEqual<string>(res[0].Value, "0", "Unexpected response value");
        }

        [TestMethod]
        public async Task CanSetCountForFloatReadOperation()
        {
            //Act
            await slaveSession.InitSession();
            var invalidOps = operations.Any(o => o.Value.DataType != ModbusDataType.Int16 && o.Value.Count != 2);

            //Assert
            Assert.IsTrue(!invalidOps, "Count for IsFloat operation was not set correctly");
        }
    }
}
