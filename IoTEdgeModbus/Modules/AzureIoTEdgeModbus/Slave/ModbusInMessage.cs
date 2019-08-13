namespace AzureIoTEdgeModbus.Slave
{
    public class ModbusInMessage
    {
        public string HwId { get; set; }
        public string UId { get; set; }
        public string Address { get; set; }
        public string Value { get; set; }
        public bool? IsSimpleValue { get; set; } = true;
        public string ValueType { get; set; }
    }
}