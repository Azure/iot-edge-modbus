using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureIoTEdgeModbus.Slave
{
    public class ModbusComplexValuesTypes
    {
        public const string Float = "float";

        public static readonly List<string> ValuesList = new List<string>() { Float };
    }

    public class ModbusComplexValues
    {
        public static Int16[] SplitComplextValue(string valueType, string value)
        {
            switch (valueType)
            {
                case ModbusComplexValuesTypes.Float:
                    return SplitFloat(value);
                default:
                    return new Int16[2];
            }
        }

        public static string MergeComplexValue(string valueType, byte[] simpleValues)
        {
            switch (valueType)
            {
                case ModbusComplexValuesTypes.Float:
                    return MergeFloat(simpleValues);

                default:
                    return "";
            }
        }

        private static Int16[] SplitFloat(string value)
        {
            float f = float.Parse(value);
            byte[] bf = BitConverter.GetBytes(f);

            var p1 = BitConverter.ToInt16(bf.SubArray(0, 2), 0);
            var p2 = BitConverter.ToInt16(bf.SubArray(2, 2), 0);

            return new Int16[] { p1, p2 };
        }

        private static string MergeFloat(byte[] values)
        {
            //If incoming message is in Little Endian (least significant byte in the beginning) we need to reverse the arrays
            var a1 = values.SubArray(0, 2).Reverse().ToArray();
            var a2 = values.SubArray(2, 2).Reverse().ToArray();

            var fl = new byte[4];
            Array.Copy(a1, 0, fl, 0, 2);
            Array.Copy(a2, 0, fl, 2, 2);
            var n = BitConverter.ToSingle(fl, 0);

            return String.Format("{0:F3}", n);
        }
    }
}
