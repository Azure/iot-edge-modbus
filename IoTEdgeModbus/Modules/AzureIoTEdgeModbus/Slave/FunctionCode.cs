namespace AzureIoTEdgeModbus.Slave
{
    public enum FunctionCode
    {
        Unknown = 0,
        ReadCoils = 1,
        ReadInputs = 2,
        ReadHoldingRegisters = 3,
        ReadInputRegisters = 4,
        WriteCoil = 5,
        WriteHoldingRegister = 6
    }
}
