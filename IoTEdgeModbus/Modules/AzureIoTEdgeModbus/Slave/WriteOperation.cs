using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AzureIoTEdgeModbus.Slave
{
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
