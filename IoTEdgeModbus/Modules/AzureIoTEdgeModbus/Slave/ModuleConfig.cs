namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class ModuleConfig
    {
        [DefaultValue(5000)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int PublishInterval { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, ModbusSlaveConfig> SlaveConfigs { get; set; }
    }
}