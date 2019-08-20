using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureIoTEdgeModbus.Slave
{
    public class ModbusFloatHandler
    {

        public static string MergeFloat(byte[] values, bool endianSwap = false)
        {
            ////Mid endian swap logic
            //var a1 = values.SubArray(0, 2).Reverse().ToArray();
            //var a2 = values.SubArray(2, 2).Reverse().ToArray();

            //var fl = new byte[4];
            //Array.Copy(a1, 0, fl, 0, 2);
            //Array.Copy(a2, 0, fl, 2, 2);

            if (endianSwap)
                values = values.Reverse().ToArray();
            return String.Format("{0:F3}", BitConverter.ToSingle(values)); 
        }

        public static Int16[] SplitFloat(float value)
        {
            //float f = float.Parse(value);
            byte[] bf = BitConverter.GetBytes(value);

            var p1 = BitConverter.ToInt16(bf.SubArray(0, 2), 0);
            var p2 = BitConverter.ToInt16(bf.SubArray(2, 2), 0);

            return new Int16[] { p1, p2 };
        }
    }
}
