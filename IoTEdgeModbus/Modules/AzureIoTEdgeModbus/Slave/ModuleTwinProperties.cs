namespace AzureIoTEdgeModbus.Slave
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This class creates a container to easily serialize the configuration so that the reported
    /// properties can be updated.
    /// </summary>
    public class ModuleTwinProperties
    {
        public ModuleTwinProperties(int? publishInterval, ModuleConfig moduleConfig)
        {
            this.PublishInterval = publishInterval;
            this.SlaveConfigs = moduleConfig?.SlaveConfigs.ToDictionary(pair => pair.Key, pair => pair.Value.AsBase());
        }

        public int? PublishInterval { get; set; }
        public Dictionary<string, BaseModbusSlaveConfig> SlaveConfigs { get; set; }
    }
}