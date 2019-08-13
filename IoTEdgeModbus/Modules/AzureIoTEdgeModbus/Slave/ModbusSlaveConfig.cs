namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// This class contains the configuration for a Modbus session.
    /// </summary>
    public class ModbusSlaveConfig : BaseModbusSlaveConfig
    {
        [JsonProperty(Required = Required.Always)]
        public new Dictionary<string, ReadOperation> Operations = null;

        public ConnectionType GetConnectionType()
        {
            if (IPAddress.TryParse(this.SlaveConnection, out IPAddress address))
            {
                return ConnectionType.ModbusTCP;
            }
            else if (this.SlaveConnection.Contains("COM") || this.SlaveConnection.Contains("/tty"))
            {
                return ConnectionType.ModbusRTU;
            }
            //TODO: ModbusRTU ModbusASCII
            return ConnectionType.Unknown;
        }

        public BaseModbusSlaveConfig AsBase()
        {
            // Unfortunately we need to create new objects since simple polymorphism will still keep the same fields of the child classes.
            var baseConfig = new BaseModbusSlaveConfig
            {
                SlaveConnection = this.SlaveConnection,
                RetryCount = this.RetryCount,
                RetryInterval = this.RetryInterval,
                TcpPort = this.TcpPort,
                HwId = this.HwId,
                BaudRate = this.BaudRate,
                StopBits = this.StopBits,
                DataBits = this.DataBits,
                Parity = this.Parity
            };

            baseConfig.Operations = this.Operations.ToDictionary(
                pair => pair.Key,
                pair => new BaseReadOperation
                {
                    PollingInterval = pair.Value.PollingInterval,
                    UnitId = pair.Value.UnitId,
                    StartAddress = pair.Value.StartAddress,
                    Count = pair.Value.Count,
                    DisplayName = pair.Value.DisplayName,
                    CorrelationId = pair.Value.CorrelationId,
                });


            return baseConfig;
        }
    }
}