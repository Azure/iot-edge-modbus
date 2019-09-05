namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Collections.Generic;
    using Data;
    using static Constants;

    /// <summary>
    /// Handles value reading of bit values from Coils and Discrete inputs.
    /// </summary>
    public class DiscreteBoolDecoder : IModbusDataDecoder
    {
        public ModbusDataType DataType => ModbusDataType.Bool;


        public IEnumerable<string> GetValues(Span<byte> bytes, int valuesToRead, SwapMode swapMode)
        {
            if (bytes.Length * BitsInByte < valuesToRead)
            {
                throw new ArgumentException("Too few bytes to read the desired number of values (bits).");
            }

            var result = new List<string>();

            foreach (byte b in bytes)
            {
                for (int i = 0; i < BitsInByte; i++)
                {
                    //Get value of each bit. Start by LSB as stated in Modbus specification.
                    var value = (b >> i) & 0b1;
                    result.Add(value.ToString());

                    if (result.Count == valuesToRead)
                        return result;
                }
            }

            // Unreachable. The assertion guarantees that we have enough bits and result is returned from loop.
            return result;

        }

        public int GetByteCount(int valuesToRead) => valuesToRead / BitsInByte + 1;

    }
}
