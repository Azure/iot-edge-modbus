namespace Azure.IoT.Edge.Modbus.Tests.Data
{
	using System;
	using System.Linq;
	using AzureIoTEdgeModbus.Slave;
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
			//Arrange
			var decoder = new Int32Decoder();
			var readOperation = new ReadOperation()
			{
				SwapMode = SwapMode.BigEndian,
				StartAddress = "40001",
				Count = 1
			};

			//Act
			var result = decoder.GetValues(bytes.AsSpan(), readOperation);

			//Assert
			Assert.AreEqual(expectedValue, result.First().Value);
			Assert.AreEqual(readOperation.StartAddress, result.First().Address.ToString());
		}
	}
}
