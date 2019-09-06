namespace Azure.IoT.Edge.Modbus.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ModbusTCPSlaveSessionTests
    {
        private ModuleConfig config;

        private ModbusTCPSlaveSession slaveSession;
        private Dictionary<string, ReadOperation> operations;

        [TestInitialize]
        public void Setup()
        {
            config = ConfigBuilder.CreateTCPOnlyConfig();
            slaveSession = new ModbusTCPSlaveSession(config.SlaveConfigs.Values.First());
            operations = config.SlaveConfigs.Values.First().Operations;

        }

        [TestMethod]
        public void ProcessResponse_SliceResponseForDecodingCorrectly()
        {
            //Arrange
            var decoder = new Mock<IModbusDataDecoder>();
            var fullResponseWithFourDataBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 4, 1, 2, 3, 4 };
            var operation = new ReadOperation()
            {
                Decoder = decoder.Object,
                Count = 1,
                Response = fullResponseWithFourDataBytes
            };

            var expectedResult = new byte[] { 1, 2, 3, 4 };
            //Act
            slaveSession.DecodeResponse(operation);

            //Assert
            decoder.Verify(d => d.GetValues(It.Is<byte[]>(param => param.SequenceEqual(expectedResult)), operation));
        }
    }
}
