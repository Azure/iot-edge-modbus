namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;

    public class Int16Decoder : ValueDecoderBase, IModbusDataDecoder
    {
        protected override int ByteSize => 2;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            return BitConverter.ToInt16(valueBytes).ToString();
        }

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;
    }
}
