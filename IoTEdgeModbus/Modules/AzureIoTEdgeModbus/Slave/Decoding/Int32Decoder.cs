using System;
using System.Collections.Generic;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public class Int32Decoder : IModbusDataDecoder
    {
        private const int ByteSize = 4;

        public ModbusDataType DataType => ModbusDataType.Int32;

        public IEnumerable<string> GetValues(Span<byte> bytes, int valuesToRead)
        {
            var result = new List<string>();

            for (int i = 0; i < valuesToRead; i++)
            {
                var valueBytes = bytes.Slice(i * ByteSize, ByteSize);

                if (BitConverter.IsLittleEndian)
                    valueBytes.Reverse();

                result.Add(BitConverter.ToInt32(valueBytes).ToString());
            }

            return result;
        }

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;
    }
}
