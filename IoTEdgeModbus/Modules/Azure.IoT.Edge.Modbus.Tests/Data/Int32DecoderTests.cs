using AzureIoTEdgeModbus.Slave.Data;
using AzureIoTEdgeModbus.Slave.Decoding;

namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class Int32DecoderTests
    {
        [DataTestMethod]
        [DataRow(40000, new byte[] { 0x0, 0x0, 0x9C, 0x40 })]
        public void CanDecodeValue(Int32 expectedValue, byte[] bytes)
        {
            var decoder = new Int32Decoder();
            var result = decoder.GetValues(bytes.AsSpan(), 1, SwapMode.None);

            Assert.AreEqual(expectedValue.ToString(), result.First());
        }
    }
}
