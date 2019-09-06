namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using Data;

    /// <summary>
    /// Swaps bytes as according to the defined Modbus swap modes.
    /// </summary>
    public static class ByteSwapper
    {
        public static void Swap(Span<byte> bytes, SwapMode swapMode )
        {
            if (bytes.Length % 2 != 0)
            {
                throw new ArgumentException("Swapping on odd number of bytes is not supported.");
            }

            switch (swapMode)
            {
                case SwapMode.BigEndian:
                    break;
                case SwapMode.LittleEndian:
                    bytes.Reverse();
                    break;
                case SwapMode.BigEndianByteSwap:
                    SwapBytes(bytes);
                    break;
                case SwapMode.LittleEndianByteSwap:
                    bytes.Reverse();
                    SwapBytes(bytes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(swapMode), swapMode, "Unsupported swap mode.");
            }
        }

        private static void SwapBytes(Span<byte> bytes)
        {
            for (int i = 0; i < bytes.Length / 2; i++)
            {
                bytes.Slice(i * 2, 2).Reverse();
            }
        }
    }
}