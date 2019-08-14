namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.IO.Ports;

    /// <summary>
    /// The base data for a Modbus session used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    public class BaseModbusSlaveConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string SlaveConnection { get; set; }

        [DefaultValue(10)]
        [IntegerValidator(MinValue = 1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? RetryCount { get; set; }

        [DefaultValue(50)]
        [IntegerValidator(MinValue = 1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? RetryInterval { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string HwId { get; set; }

        [DefaultValue(502)]
        [IntegerValidator(MinValue = 1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int? TcpPort { get; set; }

        #region Serial Communication Related Properties
        public uint? BaudRate { get; set; }

        public StopBits? StopBits { get; set; }

        public byte? DataBits { get; set; }

        public Parity? Parity { get; set; }

        [DefaultValue("NONE")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string FlowControl { get; set; }
        #endregion

        public Dictionary<string, BaseReadOperation> Operations;
    }
}