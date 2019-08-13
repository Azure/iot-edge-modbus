namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;

    using System;
    using System.Text;

    /// <summary>
    /// This class contains the configuration for a single Modbus read request.
    /// </summary>
    public class ReadOperation : BaseReadOperation
    {
        [JsonProperty(Required = Required.Default)]
        public byte[] Request;

        [JsonProperty(Required = Required.Default)]
        public byte[] Response;

        [JsonProperty(Required = Required.Default)]
        public int RequestLen;

        [JsonProperty(Required = Required.Default)]
        public byte Entity
            => (Encoding.ASCII.GetBytes(this.StartAddress, 0, 1)[0]);

        [JsonProperty(Required = Required.Default)]
        public ushort Address
            => ((ushort)(Convert.ToInt32(this.StartAddress.Substring(1)) - 1));

        [JsonProperty(Required = Required.Default)]
        public string OutFormat
            => (this.StartAddress.Length == 5 ? "{0}{1:0000}" : this.StartAddress.Length == 6 ? "{0}{1:00000}" : string.Empty);

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