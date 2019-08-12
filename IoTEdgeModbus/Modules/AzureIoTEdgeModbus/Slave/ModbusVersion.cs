namespace AzureIoTEdgeModbus.Slave
{
    using System.ComponentModel;

    public class ModbusVersion
    {
        public ModbusVersion(string version)
        {
            this.Version = version;
        }

        [DefaultValue("2")]
        public string Version { get; set; }
    }
}