using System;
using System.Collections.Generic;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public class Int16Decoder : IModbusDataDecoder
    {
        public ModbusDataType DataType => ModbusDataType.Int16;

        private const int ByteSize = 2;

        public IEnumerable<string> GetValues(Span<byte> bytes, int valuesToRead)
        {
            var result = new List<string>();

            for (int i = 0; i < valuesToRead; i++)
            {
                var slice = bytes.Slice(i * ByteSize, ByteSize);

                // TODO: We need to test this on a BigEndian architecture
                if (BitConverter.IsLittleEndian)
                    slice.Reverse();
                
                result.Add(BitConverter.ToInt16(slice).ToString());
            }

            return result;
        }

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;
    }
}
