namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class ModbusOutContent
    {
        public string HwId { get; set; }

        public List<ModbusOutData> Data { get; set; }

        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> AdditionalProperties { get; set; }
    }
}
