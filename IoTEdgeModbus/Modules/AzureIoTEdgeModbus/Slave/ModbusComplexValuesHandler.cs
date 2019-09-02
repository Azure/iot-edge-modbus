using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace AzureIoTEdgeModbus.Slave
{
    public class ModbusComplexValuesHandler
    {

        public static Int16[] SplitComplexValue(float value, ModbusValueType valueType)
        {
            throw new NotImplementedException();
        }

        public static string MergeComplexValue(byte[] values, ModbusValueType valueType, bool endianSwap = false, bool midEndianSwap = false)
        {
            //In case of adding new value type, modify endian handling logic
            if(midEndianSwap && (valueType == ModbusValueType.Float || valueType == ModbusValueType.Int32))
            {
                //Mid endian swap logic
                Array.Reverse(values, 0,2);
                Array.Reverse(values, 2, 2);

            }
            if (endianSwap)
                Array.Reverse(values);

            switch (valueType)
            {
                case ModbusValueType.Float:
                    return String.Format("{0:F3}", BitConverter.ToSingle(values));
                case ModbusValueType.Int32:
                    return BitConverter.ToInt32(values).ToString();
                default:
                    return "Uknown value type";
            }

        }

        private static Int16[] SplitComplexValue(string value, ModbusValueType valueType)
        {
            byte[] bf;
            //float f = float.Parse(value);
            switch(valueType){
                case ModbusValueType.Float:
                    bf = BitConverter.GetBytes(float.Parse(value));
                    break;
                case ModbusValueType.Int32:
                    bf = BitConverter.GetBytes(Convert.ToInt32(value));
                    break;
                default:
                    return null;
            }

            //At this point all complex values used need only 2 registries (2 Int16)
            var p1 = BitConverter.ToInt16(bf.SubArray(0, 2), 0);
            var p2 = BitConverter.ToInt16(bf.SubArray(2, 2), 0);

            return new Int16[] { p1, p2 };
        }

        internal static ushort GetCountForValueType(ModbusValueType valueType)
        {
            return RegistryCountsForValue.GetValueOrDefault(valueType);
        }


        //In case of adding new complex value type, provide number of registries to use here
        private static Dictionary<ModbusValueType, ushort> RegistryCountsForValue = new Dictionary<ModbusValueType, ushort>()
        {
            { ModbusValueType.Float, 2 },
            { ModbusValueType.Int32, 2 }
        };

    }
}
