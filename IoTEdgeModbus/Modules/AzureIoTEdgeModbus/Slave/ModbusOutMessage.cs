namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;

    public class ModbusOutMessage
    {
        public string PublishTimestamp { get; set; }
        public List<ModbusOutContent> Content { get; set; }
    }
}