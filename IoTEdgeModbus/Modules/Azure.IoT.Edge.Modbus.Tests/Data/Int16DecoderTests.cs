namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System;
    using System.Linq;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Int16DecoderTests
    {
        [TestMethod]
        public void CanDecodeValue()
        {
            //Arrange
            var expectedValue = "1234";
            var response = new byte[] {0x4, 0xD2};
            var bytes = new Span<byte>(response);
            var decoder = new Int16Decoder();
            var readOperation = new ReadOperation()
            {
                SwapMode = SwapMode.BigEndian,
                StartAddress = "40001",
                Count = 1

            };

            //Act
            var result = decoder.GetValues(bytes, readOperation );

            //Assert
            Assert.AreEqual(expectedValue, result.First().Value);
            Assert.AreEqual(readOperation.StartAddress, result.First().Address.ToString());
        }
    }
}
