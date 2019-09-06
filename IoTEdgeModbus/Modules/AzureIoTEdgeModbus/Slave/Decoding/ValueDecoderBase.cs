namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Collections.Generic;
    using Data;

    public abstract class ValueDecoderBase
    {
        protected abstract int ByteSize { get; }

        public IEnumerable<DecodedValue> GetValues(byte[] bytesToConvert, ReadOperation operation)
        {
            var byteSpan = new Span<byte>(bytesToConvert);
            var result = new List<DecodedValue>();

            for (int i = 0; i < operation.Count; i++)
            {
                var valueIndex = i * this.ByteSize;
                var valueBytes = byteSpan.Slice(valueIndex, this.ByteSize);

                ByteSwapper.Swap(valueBytes, operation.SwapMode);

                if (BitConverter.IsLittleEndian)
                    valueBytes.Reverse();

                var address = Convert.ToInt32(operation.StartAddress) + valueIndex;

                result.Add(new DecodedValue(address, this.ConvertToString(valueBytes)));
            }

            return result;
        }

        protected abstract string ConvertToString(in Span<byte> valueBytes);
    }
}