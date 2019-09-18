namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System.Collections.Generic;

    public interface IModbusDataDecoder
    {
        /// <summary>
        /// Get decoded values from raw bytes.
        /// </summary>
        /// <remarks>
        /// Note that the byte order of the referenced bytes might be changed after completion of this method.
        /// </remarks>
        IEnumerable<DecodedValue> GetValues(byte[] bytesToConvert, ReadOperation operation);

        /// <summary>
        /// Gets the number of entities (coils, registers etc) to read for the specified number of values.
        /// </summary>
        short GetEntityCount(short valuesToRead);
    }
}
