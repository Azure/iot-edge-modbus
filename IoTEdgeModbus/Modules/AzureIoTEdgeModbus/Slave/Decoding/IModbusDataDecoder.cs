using System;
using System.Collections.Generic;
using AzureIoTEdgeModbus.Slave.Data;

namespace AzureIoTEdgeModbus.Slave.Decoding
{
    public interface IModbusDataDecoder
    {
        ModbusDataType DataType { get; }
        
        // TODO: Add swap mode parameter
        /// <summary>
        /// Get values
        /// </summary>
        /// <remarks>
        /// Note that the byte order of the referenced bytes might be changed after completion of this method.
        /// </remarks>
        IEnumerable<string> GetValues(Span<byte> bytes, int valuesToRead);

        int GetByteCount(int valuesToRead);
    }
}
