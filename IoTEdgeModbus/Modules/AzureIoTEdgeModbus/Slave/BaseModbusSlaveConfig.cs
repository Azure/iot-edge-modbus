namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.IO.Ports;

    /// <summary>
    /// The base data for a Modbus session used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    public class BaseModbusSlaveConfig
    {
        public string SlaveConnection { get; set; }

        [DefaultValue(10)]
        [IntegerValidator(MinValue = 1)]
        public int? RetryCount { get; set; }

        [DefaultValue(50)]
        [IntegerValidator(MinValue = 1)]
        public int? RetryInterval { get; set; }

        [DefaultValue(502)]
        [IntegerValidator(MinValue = 1)]
        public int? TcpPort { get; set; }

        public string HwId { get; set; }

        public uint? BaudRate { get; set; }

        public StopBits? StopBits { get; set; }

        public byte? DataBits { get; set; }

        public Parity? Parity { get; set; }

        public Dictionary<string, BaseReadOperation> Operations;
    }
}