namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using Decoding;

    /// <summary>
    /// This class contains the configuration for a single Modbus read request.
    /// </summary>
    public class ReadOperation : ModbusOperation
    {
        [DefaultValue(3000)]
        [Range(500, 50000)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int PollingInterval { get; set; }

        [Range(1, 32)]
        [JsonProperty(Required = Required.Always)]
        public short Count { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string DisplayName { get; set; }

        [DefaultValue("DefaultCorrelationId")]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public string CorrelationId { get; set; }

        [JsonIgnore]
        public byte[] Request { get; set; }

        [JsonIgnore]
        public byte[] Response { get; set; }

        [JsonIgnore]
        public int RequestLength { get; set; }

        [JsonIgnore]
        public IModbusDataDecoder Decoder { get; set; }
    }
}