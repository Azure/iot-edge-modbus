namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;

    public class Int32Decoder : ValueDecoderBase, IModbusDataDecoder
    {
        protected override short ByteSize => 4;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            return BitConverter.ToInt32(valueBytes).ToString();
        }

        public short GetByteCount(short valuesToRead)
            => checked((short)(ByteSize * valuesToRead));
    }
}
