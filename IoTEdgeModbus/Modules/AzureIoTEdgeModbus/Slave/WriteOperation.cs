namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using Newtonsoft.Json;

    public class WriteOperation : ModbusOperation
    {
        [JsonProperty(Required = Required.Always)]
        public string HwId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float Value { get; set; }

        [JsonIgnore]
        public UInt16 IntValueToWrite { get; set; }
    }
}
