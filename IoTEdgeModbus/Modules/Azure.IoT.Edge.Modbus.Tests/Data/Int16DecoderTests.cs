namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System;
    using System.Linq;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Int16DecoderTests
    {
        [TestMethod]
        public void CanDecodeValue()
        {
            var expectedValue = "1234";
            var response = new byte[] {0x4, 0xD2};
            var bytes = new Span<byte>(response);
            var decoder = new Int16Decoder();
            var result = decoder.GetValues(bytes, 1, SwapMode.BigEndian);

            Assert.AreEqual(expectedValue, result.First());
        }
    }
}
