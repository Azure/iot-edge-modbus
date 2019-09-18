namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System;
    using System.Linq;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ByteSwapperTests
    {
        [TestClass]
        public class Swap
        {
            [DataTestMethod]
            [DataRow("ABCD", SwapMode.BigEndian, "ABCD")]
            [DataRow("BADC", SwapMode.BigEndianByteSwap, "ABCD")]
            [DataRow("DCBA", SwapMode.LittleEndian, "ABCD")]
            [DataRow("CDAB", SwapMode.LittleEndianByteSwap, "ABCD")]
            [DataRow("ABCDEFGH", SwapMode.BigEndian, "ABCDEFGH")]
            [DataRow("BADCFEHG", SwapMode.BigEndianByteSwap, "ABCDEFGH")]
            [DataRow("HGFEDCBA", SwapMode.LittleEndian, "ABCDEFGH")]
            [DataRow("GHEFCDAB", SwapMode.LittleEndianByteSwap, "ABCDEFGH")]
            public void SwapsCorrectly(string expectedValue, SwapMode swapMode, string toConvert)
            {
                var bytes = toConvert.Select(c => (byte)c).ToArray().AsSpan();

                ByteSwapper.Swap(bytes, swapMode);

                var result = new string(bytes.ToArray().Select(b => (char)b).ToArray());

                Assert.AreEqual(expectedValue, result);
            }
        }
    }
}
