using System;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public class Int32Decoder : ValueDecoderBase, IModbusDataDecoder
    {
        public ModbusDataType DataType => ModbusDataType.Int32;

        protected override int ByteSize => 4;

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            return BitConverter.ToInt32(valueBytes).ToString();
        }
    }
}
