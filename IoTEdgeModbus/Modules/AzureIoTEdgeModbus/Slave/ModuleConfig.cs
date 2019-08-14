namespace AzureIoTEdgeModbus.Slave
{
    using AzureIoTEdgeModbus.Configuration;
    using DotNetty.Common.Utilities;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;

    public class ModuleConfig
    {
        [DefaultValue(5000)]
        [IntegerValidator(MinValue = 1)]
        public int PublishInterval { get; set; }

        public Dictionary<string, ModbusSlaveConfig> SlaveConfigs { get; set; }
    }
}