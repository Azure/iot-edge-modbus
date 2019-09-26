namespace AzureIoTEdgeModbus.Slave
{
    public class ModbusOutMessageV1
    {
        public string DisplayName { get; set; }
        public string HwId { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
        public string SourceTimestamp { get; set; }
    }
}