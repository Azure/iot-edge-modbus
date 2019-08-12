namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using System.Text;

    /// <summary>
    /// This class contains the configuration for a single Modbus read request.
    /// </summary>
    public class ReadOperation : BaseReadOperation
    {
        public byte[] Request;
        public byte[] Response;
        public int RequestLen;

        public byte Entity
            => (Encoding.ASCII.GetBytes(this.StartAddress, 0, 1)[0]);

        public ushort Address
            => ((ushort)(Convert.ToInt32(this.StartAddress.Substring(1)) - 1));

        public string OutFormat
            => (this.StartAddress.Length == 5 ? "{0}{1:0000}" : this.StartAddress.Length == 6 ? "{0}{1:00000}" : string.Empty);

        /// <summary>
        /// Only Read Supported
        /// </summary>
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