namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;

    public class ModbusOutContent
    {
        public string HwId { get; set; }
        public List<ModbusOutData> Data { get; set; }
    }
}
