namespace AzureIoTEdgeModbus.Slave.Decoding
{
    using System;
    using System.Collections.Generic;
    using Data;

    public interface IModbusDataDecoder
    {
        /// <summary>
        /// Get values
        /// </summary>
        /// <remarks>
        /// Note that the byte order of the referenced bytes might be changed after completion of this method.
        /// </remarks>
        IEnumerable<DecodedValue> GetValues(byte[] bytesToConvert, ReadOperation operation);

        int GetByteCount(int valuesToRead);
    }
}
