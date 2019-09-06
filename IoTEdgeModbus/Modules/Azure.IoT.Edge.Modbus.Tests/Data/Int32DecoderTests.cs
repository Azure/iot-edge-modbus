namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System;
    using System.Linq;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Int32DecoderTests
    {
        [DataTestMethod]
        [DataRow("40000", new byte[] { 0x0, 0x0, 0x9C, 0x40 })]
        [DataRow("70000", new byte[] { 0x0, 0x1, 0x11, 0x70 })]
        public void CanDecodeValue(string expectedValue, byte[] bytes)
        {
            var decoder = new Int32Decoder();
            var result = decoder.GetValues(bytes.AsSpan(), 1, SwapMode.BigEndian);

            Assert.AreEqual(expectedValue, result.First());
        }
    }
}
