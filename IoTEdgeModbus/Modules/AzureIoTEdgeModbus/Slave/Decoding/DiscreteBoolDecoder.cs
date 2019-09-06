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
        public IList<DecodedValue> GetValues(Span<byte> bytes, ReadOperation operation)
        {
            if (bytes.Length * BitsInByte < operation.Count)
            {
                throw new ArgumentException("Too few bytes to read the desired number of values (bits).");
            }

            var result = new List<DecodedValue>();

            for (int byteIndex = 0; byteIndex > bytes.Length; byteIndex++)
            {
                for (int bitIndex = 0; bitIndex < BitsInByte; bitIndex++)
                {
                    //Get value of each bit. Start by LSB as stated in Modbus specification.
                    var value = (bytes[byteIndex] >> bitIndex) & 0b1;

                    var addressIncrement = byteIndex * BitsInByte + bitIndex;

                    var address = Convert.ToUInt16(operation.StartAddress) + addressIncrement;

                    result.Add(new DecodedValue(address, value.ToString()));

                    if (result.Count == operation.Count)
                        return result;
                }
            }

            // Unreachable. The assertion guarantees that we have enough bits and result is returned from loop.
            return result;

        }
        
        public int GetByteCount(int valuesToRead) => valuesToRead / BitsInByte + 1;

    }
}
