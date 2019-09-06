namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public struct DecodedModbusValue
    {
        public DecodedModbusValue(string address, string value)
        {
            this.Address = address;
            this.Value = value;
        }

        public string Address { get; }

        public string Value { get; }
    }
}