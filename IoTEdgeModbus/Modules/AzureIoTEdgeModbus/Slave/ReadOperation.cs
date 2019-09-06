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

        [Range(1, 20)]
        [JsonProperty(Required = Required.Always)]
        public UInt16 Count { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string DisplayName { get; set; }

        [DefaultValue("DefaultCorrelationId")]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public string CorrelationId { get; set; }

        [JsonProperty(Required = Required.Default)]
        public byte[] Request { get; set; }

        [JsonProperty(Required = Required.Default)]
        public byte[] Response { get; set; }

        [JsonProperty(Required = Required.Default)]
        public int RequestLength { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string OutFormat
            => (this.StartAddress.Length == 5 ? "{0}{1:0000}" : this.StartAddress.Length == 6 ? "{0}{1:00000}" : string.Empty);

        [JsonProperty(Required = Required.Default)]
        public IModbusDataDecoder Decoder { get; set; }
    }
}