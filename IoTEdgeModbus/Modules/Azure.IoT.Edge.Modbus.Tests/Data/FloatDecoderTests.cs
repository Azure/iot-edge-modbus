
namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System.Linq;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FloatDecoderTests
    {
        private FloatDecoder decoder;
        [TestInitialize]
        public void Setup()
        {
            this.decoder = new FloatDecoder();
        }

        [TestClass]
        public class GetValues : FloatDecoderTests
        {
            [DataTestMethod]
            [DataRow("5.5", new byte[] {64, 176, 0, 0})]
            [DataRow("4000.555", new byte[] { 69, 122, 8, 227 })]
            public void CanDecodeValue(string expectedValue, byte[] bytes)
            {
                //Arrange
                var readOperation = new ReadOperation
                {
                    SwapMode = SwapMode.BigEndian,
                    StartAddress = "40001",
                    Count = 1
                };

                //Act
                var result = this.decoder.GetValues(bytes, readOperation).ToList();

                //Assert
                Assert.AreEqual(expectedValue, result.First().Value);
                Assert.AreEqual(readOperation.StartAddress, result.First().Address.ToString());
            }
        }

        [TestClass]
        public class GetEntityCount : FloatDecoderTests
        {
            [TestMethod]
            public void ReturnsCorrectNumberOfEntities()
            {
                //Arrange
                short valuesToRead = 5;
                short expectedResult = 10;
                //Act
                var result = this.decoder.GetEntityCount(valuesToRead);

                //Assert
                Assert.AreEqual(expectedResult, result);
            }
        }
    }
}

