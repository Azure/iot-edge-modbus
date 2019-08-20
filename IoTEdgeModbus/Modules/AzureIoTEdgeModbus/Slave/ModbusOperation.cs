namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Text;

    /// <summary>
    /// Base Modbus Operation Class
    /// </summary>
    public class ModbusOperation
    {
        [JsonProperty(Required = Required.Always)]
        public byte UnitId { get; set; }

        [MinLength(5)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public string StartAddress { get; set; }

        [DefaultValue(false)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsFloat { get; set; }

        [JsonProperty(Required = Required.Default)]
        public byte Entity
         => (Encoding.ASCII.GetBytes(this.StartAddress, 0, 1)[0]);

        [JsonProperty(Required = Required.Default)]
        public ushort Address
            => ((ushort)(Convert.ToInt32(this.StartAddress.Substring(1)) - 1));

        /// <summary>
        /// Only Read Supported
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public byte FunctionCode
           => (
                (char)this.Entity == (char)EntityType.CoilStatus ? (byte)FunctionCodeType.ReadCoils :
                (char)this.Entity == (char)EntityType.HoldingRegister ? (byte)FunctionCodeType.ReadHoldingRegisters :
                (char)this.Entity == (char)EntityType.InputStatus ? (byte)FunctionCodeType.ReadInputs :
                (char)this.Entity == (char)EntityType.InputRegister ? (byte)FunctionCodeType.ReadInputRegisters :
                byte.MinValue
            );


    }
}