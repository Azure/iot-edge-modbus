namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    public class ModuleConfig
    {
        [DefaultValue(5000)]
        [Range(1, int.MaxValue)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int PublishInterval { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, ModbusSlaveConfig> SlaveConfigs { get; set; }
    }
}