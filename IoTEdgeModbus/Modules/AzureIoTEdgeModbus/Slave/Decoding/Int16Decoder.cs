using System;
using System.Collections.Generic;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public class Int16Decoder : ValueDecoderBase, IModbusDataDecoder
    {
        public ModbusDataType DataType => ModbusDataType.Int16;

        protected override int ByteSize => 2;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            return BitConverter.ToInt16(valueBytes).ToString();
        }

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;
    }
}
