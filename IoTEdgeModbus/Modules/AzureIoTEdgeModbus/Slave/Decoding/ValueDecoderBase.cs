namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Collections.Generic;
    using Data;

    public abstract class ValueDecoderBase
    {
        protected abstract int ByteSize { get; }

        public IEnumerable<string> GetValues(Span<byte> bytes, int valuesToRead, SwapMode swapMode)
        {
            var result = new List<string>();

            for (int i = 0; i < valuesToRead; i++)
            {
                var valueBytes = bytes.Slice(i * this.ByteSize, this.ByteSize);

                ByteSwapper.Swap(valueBytes, swapMode);
                
                if (BitConverter.IsLittleEndian)
                    valueBytes.Reverse();

                result.Add(this.ConvertToString(valueBytes));
            }

            return result;
        }

        protected abstract string ConvertToString(in Span<byte> valueBytes);
    }
}