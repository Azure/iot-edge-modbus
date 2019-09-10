namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.IO.Ports;

    /// <summary>
    /// The base data for a Modbus session used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    public class BaseModbusSlaveConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string SlaveConnection { get; set; }

        [DefaultValue(10)]
        [Range(1, int.MaxValue)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? RetryCount { get; set; }

        [DefaultValue(50)]
        [Range(1, int.MaxValue)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? RetryInterval { get; set; }

        [DefaultValue(502)]
        [Range(1, int.MaxValue)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? TcpPort { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string HwId { get; set; }

        [JsonProperty(Required = Required.Default)]
        public uint? BaudRate { get; set; }

        [JsonProperty(Required = Required.Default)]
        public StopBits? StopBits { get; set; }

        [JsonProperty(Required = Required.Default)]
        public byte? DataBits { get; set; }

        [JsonProperty(Required = Required.Default)]
        public Parity? Parity { get; set; }
        
        [JsonProperty(Required = Required.Default)]
        public Dictionary<string, string> AdditionalProperties { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, ReadOperation> Operations { get; set; }
    }
}