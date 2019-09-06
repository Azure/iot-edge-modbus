namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;

    public struct DecodedValue
    {
        public DecodedValue(int address, string value)
        {
            this.Address = address;
            this.Value = value;
        }
        public int Address { get; }
        public string Value { get; }
    }
}