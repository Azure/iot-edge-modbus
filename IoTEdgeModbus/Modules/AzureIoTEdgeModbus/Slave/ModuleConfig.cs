namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;
    using System.ComponentModel;

    public class ModuleConfig
    {
        [DefaultValue(5000)]
        public int PublishInterval { get; set; }

        public Dictionary<string, ModbusSlaveConfig> SlaveConfigs { get; set; }
    }
}