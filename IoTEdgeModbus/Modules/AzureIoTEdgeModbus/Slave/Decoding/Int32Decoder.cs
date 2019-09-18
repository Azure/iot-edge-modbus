namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;

    public class Int32Decoder : ValueDecoderBase
    {
        protected override short ByteSize => 4;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            return BitConverter.ToInt32(valueBytes).ToString();
        }
    }
}
