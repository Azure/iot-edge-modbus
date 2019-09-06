namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;

    public class Int32Decoder : ValueDecoderBase, IModbusDataDecoder
    {
        protected override int ByteSize => 4;

        public int GetByteCount(int valuesToRead)
            => ByteSize * valuesToRead;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            return BitConverter.ToInt32(valueBytes).ToString();
        }
    }
}
