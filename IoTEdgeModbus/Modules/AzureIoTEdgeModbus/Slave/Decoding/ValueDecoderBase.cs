namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Collections.Generic;
    using Data;

    public abstract class ValueDecoderBase
    {
        protected abstract int ByteSize { get; }

        public IList<DecodedValue> GetValues(Span<byte> bytes, ReadOperation operation)
        {
            var result = new List<DecodedValue>();

            for (int i = 0; i < operation.Count; i++)
            {
                var valueIndex = i * this.ByteSize;
                var valueBytes = bytes.Slice(valueIndex, this.ByteSize);

                ByteSwapper.Swap(valueBytes, operation.SwapMode);

                if (BitConverter.IsLittleEndian)
                    valueBytes.Reverse();

                var address = Convert.ToUInt16(operation.StartAddress) + valueIndex;

                result.Add(new DecodedValue(address, this.ConvertToString(valueBytes)));
            }

            return result;
        }

        protected abstract string ConvertToString(in Span<byte> valueBytes);
    }
}