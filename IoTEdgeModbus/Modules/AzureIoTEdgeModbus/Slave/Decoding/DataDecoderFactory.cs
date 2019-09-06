namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using Data;

    public static class DataDecoderFactory
    {
        public static IModbusDataDecoder CreateDecoder(FunctionCode functionCode, ModbusDataType dataType)
        {
            switch (dataType)
            {
                case var _ when functionCode == FunctionCode.ReadCoils:
                    return new DiscreteBoolDecoder();
                case ModbusDataType.Int16:
                    return new Int16Decoder();
                case ModbusDataType.Int32:
                    return new Int32Decoder();
                case ModbusDataType.Float:
                    return new FloatDecoder();
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
    }
}
