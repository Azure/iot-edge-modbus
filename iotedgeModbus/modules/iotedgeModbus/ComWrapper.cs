using System;
using System.Runtime.InteropServices;

namespace tempSerialPort
{    
    public static class ComWrapper
    {
        public static void OutPutByteArray(byte[] arr)
        {
            int count = 0;
            foreach (byte x in arr)
            {
                Console.Write($"{x.ToString()} ");
                count++;
                if (count == 4)
                {
                    Console.WriteLine("");
                    count = 0;
                }
            }
        }
        public static byte[] StructureToByteArray<T>(T obj)
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        [DllImport("libcomWrapper.so")]
        public static extern int com_open(string pathname);

        [DllImport("libcomWrapper.so")]
        public static extern int com_close(int fd);

        [DllImport("libcomWrapper.so")]
        public static extern int com_read(int fd, IntPtr buf, int count);

        [DllImport("libcomWrapper.so")]
        public static extern int com_write(int fd, IntPtr buf, int count);

        [DllImport("libcomWrapper.so")]
        public static extern int com_tciflush(int fd);

        [DllImport("libcomWrapper.so")]
        public static extern int com_tcoflush(int fd);

        [DllImport("libcomWrapper.so")]
        public static extern int com_set_interface_attribs(int fd, int speed, int data_bits, int parity_bit, int stop_bit);
    }
}