namespace AzureIoTEdgeModbus.Slave.Data
{
    /// <summary>
    /// Byte swap modes used for complex types.
    /// </summary>
    public enum SwapMode
    {
        None,           // source bytes: [a b] [c d] target bytes: [a b c d]
        ByteAndWord,    // source bytes: [a b] [c d] target bytes: [d c b a]
        Byte,           // source bytes: [a b] [c d] target bytes: [b a d c]
        Words           // source bytes: [a b] [c d] target bytes: [c d a b]
    }
}
