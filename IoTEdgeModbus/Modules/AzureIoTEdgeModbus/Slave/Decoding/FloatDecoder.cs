using System;
using System.Collections.Generic;
using System.Globalization;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public class FloatDecoder : IModbusDataDecoder
    {
        private const int ByteSize = 4;

        public ModbusDataType DataType => ModbusDataType.Float;

        public IEnumerable<string> GetValues(Span<byte> bytes, int valuesToRead)
        {
            var result = new List<string>();

            const int decimals = 3;
            for (int i = 0; i < valuesToRead; i++)
            {
                var valueBytes = bytes.Slice(i * ByteSize, ByteSize);
                
                if (BitConverter.IsLittleEndian)
                    valueBytes.Reverse();

                var roundedValue = Math.Round(BitConverter.ToSingle(valueBytes), decimals);
                result.Add(roundedValue.ToString(CultureInfo.InvariantCulture));
            }

            return result;
        }

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;
    }
}
