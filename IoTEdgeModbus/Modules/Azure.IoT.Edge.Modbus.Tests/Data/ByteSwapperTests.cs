namespace Azure.IoT.Edge.Modbus.Tests.Data
{
	using AzureIoTEdgeModbus.Slave.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ByteSwapperTests
	{
		[DataTestMethod]
		[DataRow(40000, new byte[] { 0x0, 0x0, 0x9C, 0x40 })]
		public void SwapByte(SwapMode swapMode, byte[] bytes)
		{

		}
	}
}
