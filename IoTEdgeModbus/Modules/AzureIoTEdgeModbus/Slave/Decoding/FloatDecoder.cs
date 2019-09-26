namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Globalization;

    public class FloatDecoder : ValueDecoderBase
    {
        protected override short ByteSize => 4;

        protected override string ConvertToString(in Span<byte> valueBytes)
        {
            const int decimals = 3;
            var roundedValue = Math.Round(BitConverter.ToSingle(valueBytes), decimals);
            return roundedValue.ToString(CultureInfo.InvariantCulture);
        }
    }
}
