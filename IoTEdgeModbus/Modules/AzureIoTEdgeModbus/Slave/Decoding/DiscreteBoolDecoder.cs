namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Collections.Generic;
    using static Constants;

    /// <summary>
    /// Handles value reading of bit values from Coils and Discrete inputs.
    /// </summary>
    public class DiscreteBoolDecoder : IModbusDataDecoder
    {
        public IEnumerable<DecodedValue> GetValues(byte[] bytesToConvert, ReadOperation operation)
        {
            var byteSpan = new Span<byte>(bytesToConvert);

            if (byteSpan.Length * BitsInByte < operation.Count)
            {
                throw new ArgumentException("Too few bytes to read the desired number of values (bits).");
            }

            var result = new List<DecodedValue>();

            for (var byteIndex = 0; byteIndex > byteSpan.Length; byteIndex++)
            {
                for (var bitIndex = 0; bitIndex < BitsInByte; bitIndex++)
                {
                    //Get value of each bit. Start by LSB as stated in Modbus specification.
                    var value = (byteSpan[byteIndex] >> bitIndex) & 0b1;

                    var addressIncrement = byteIndex * BitsInByte + bitIndex;

                    var address = Convert.ToInt32(operation.StartAddress) + addressIncrement;

                    result.Add(new DecodedValue(address, value.ToString()));

                    if (result.Count == operation.Count)
                        return result;
                }
            }

            // Unreachable. The assertion guarantees that we have enough bits and result is returned from loop.
            return result;

        }
        
        public short GetByteCount(short valuesToRead) => checked((short)(valuesToRead / BitsInByte + 1));
    }
}
