namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Text;
    using Data;

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

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ModbusDataType DataType { get; set; }

        [DefaultValue(Data.SwapMode.None)]
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        public SwapMode SwapMode { get; set; }

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
        public FunctionCode FunctionCode
            => (char)this.Entity == (char)EntityType.CoilStatus ? FunctionCode.ReadCoils :
                (char)this.Entity == (char)EntityType.HoldingRegister ? FunctionCode.ReadHoldingRegisters :
                (char)this.Entity == (char)EntityType.InputStatus ? FunctionCode.ReadInputs :
                (char)this.Entity == (char)EntityType.InputRegister ? FunctionCode.ReadInputRegisters :
                FunctionCode.Unknown;
    }
}