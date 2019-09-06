namespace AzureIoTEdgeModbus.Slave.Data
{
    /// <summary>
    /// Byte swap modes used for complex types.
    /// </summary>
    public enum SwapMode
    {
        BigEndian,              // Source bytes: [a b] [c d] target bytes: [a b c d]
        LittleEndian,           // Source bytes: [a b] [c d] target bytes: [d c b a]
        BigEndianByteSwap,      // Source bytes: [a b] [c d] target bytes: [b a d c]
        LittleEndianByteSwap    // Source bytes: [a b] [c d] target bytes: [c d a b]
    }
}
