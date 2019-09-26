namespace Azure.IoT.Edge.Modbus.Tests.Data
{
    using System;
    using System.Linq;
    using AutoFixture;
    using AzureIoTEdgeModbus.Slave;
    using AzureIoTEdgeModbus.Slave.Data;
    using AzureIoTEdgeModbus.Slave.Decoding;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DiscreteBoolDecoderTests
    {
        private DiscreteBoolDecoder decoder;

        [TestInitialize]
        public void Setup()
        {
            this.decoder = new DiscreteBoolDecoder();
        }

        [TestClass]
        public class GetValues : DiscreteBoolDecoderTests
        {
            [DataTestMethod]
            [DataRow(new[] {"True", "True"}, new byte[] {3})]
            [DataRow(new[] {"False", "True", "False", "True"}, new byte[] {10})]
            [DataRow(new[] {"True", "True", "True", "True", "True", "True", "True", "True", "True"},
                new byte[] {255, 1})]
            public void CanDecodeValue(string[] expectedValues, byte[] bytes)
            {
                //Arrange
                var readOperation = new ReadOperation
                {
                    DataType = ModbusDataType.Bool,
                    SwapMode = SwapMode.BigEndian,
                    StartAddress = "00001",
                    Count = (short)expectedValues.Length
                };

                //Act
                var result = this.decoder.GetValues(bytes, readOperation).ToList();

                //Assert
                CollectionAssert.AreEqual(expectedValues, result.Select(r => r.Value).ToArray());
                Assert.IsTrue(result.Select((res, index) => new
                {
                    Index = index,
                    Address = res.Address
                }).All(ir => ir.Address == (1 + ir.Index)));
            }

            [TestMethod]
            public void ThrowsArgumentException_WhenNotEnoughDataToExtractValues()
            {

                var bytes = new byte[1];
                //Arrange
                var readOperation = new ReadOperation
                {
                    DataType = ModbusDataType.Bool,
                    SwapMode = SwapMode.BigEndian,
                    StartAddress = "00001",
                    Count = 9
                };

                //Act and Assert
                Assert.ThrowsException<ArgumentException>(() => this.decoder.GetValues(bytes, readOperation));
            }
        }

        [TestClass]
        public class GetEntityCount : DiscreteBoolDecoderTests
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
