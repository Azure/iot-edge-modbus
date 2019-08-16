namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The base data for a read operation used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    public class BaseReadOperation
    {
        [DefaultValue(3000)]
        [Range(500, 50000)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int PollingInterval { get; set; }

        [JsonProperty(Required = Required.Always)]
        public byte UnitId { get; set; }


        [MinLength(5)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public string StartAddress { get; set; }

        [Range(1, 20)]
        [JsonProperty(Required = Required.Always)]
        public UInt16 Count { get; set; }


        [JsonProperty(Required = Required.Always)]
        public string DisplayName { get; set; }

        [DefaultValue("DefaultCorrelationId")]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public string CorrelationId { get; set; }
    }
}