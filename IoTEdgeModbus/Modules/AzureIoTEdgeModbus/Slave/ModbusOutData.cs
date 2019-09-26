namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;

    public class ModbusOutData
    {
        public string CorrelationId { get; set; }
        public string SourceTimestamp { get; set; }
        public List<ModbusOutValue> Values { get; set; }
    }
}