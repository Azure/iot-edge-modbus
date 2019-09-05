using System;
using System.Globalization;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public class FloatDecoder : ValueDecoderBase, IModbusDataDecoder
    {
        public ModbusDataType DataType => ModbusDataType.Float;

        protected override int ByteSize => 4;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            const int decimals = 3;
            var roundedValue = Math.Round(BitConverter.ToSingle(valueBytes), decimals);
            return roundedValue.ToString(CultureInfo.InvariantCulture);
        }

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;
    }
}
