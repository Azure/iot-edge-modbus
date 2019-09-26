namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System.Linq;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Int16DecoderTests
    {
        private Int16Decoder decoder;
        [TestInitialize]
        public void Setup()
        {
            this.decoder = new Int16Decoder();
        }

        [TestClass]
        public class GetValues : Int16DecoderTests
        {
            [DataTestMethod]
            [DataRow("1234", new byte[] {0x4, 0xD2})]
            public void CanDecodeValue(string expectedValue, byte[] bytes)
            {
                //Arrange
                var decoder = new Int16Decoder();
                var readOperation = new ReadOperation()
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
        public class GetEntityCount : Int16DecoderTests
        {
            [TestMethod]
            public void ReturnsCorrectNumberOfEntities()
            {
                //Arrange
                short valuesToRead = 5;
                short expectedResult = 5;
                //Act
                var result = this.decoder.GetEntityCount(valuesToRead);

                //Assert
                Assert.AreEqual(expectedResult, result);
            }
        }
    }
}
