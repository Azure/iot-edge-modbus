using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace tempSerialPort
{
    using System.IO.Ports;
    public interface ISerialDevice
    {
        void Open();
        void Close();
        void Write(byte[] buf, int offset, int len);
        int Read(byte[] buf, int offset, int len);
        bool IsOpen();
        void DiscardInBuffer();
        void DiscardOutBuffer();
        void Dispose();
    }
    public class WinSerialDevice : ISerialDevice
    {
        private SerialPort serialPort = null;
        public static WinSerialDevice CreateDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            List<string> serial_ports = new List<string>();

            // Are we on Windows?
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WinSerialDevice(portName, baudRate, parity, dataBits, stopBits);
            }
            else
            {
                Console.WriteLine("WinSerialDevice only supports Windows system");
                return null;
            }
        }

        public void Open()
        {
            this.serialPort.Open();
            this.serialPort.Handshake = Handshake.None;
        }

        private WinSerialDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        public void Close()
        {
            serialPort.Close();
        }

        public void Write(byte[] buf, int offset, int len)
        {
            serialPort.Write(buf, offset, len);
        }

        public int Read(byte[] buf, int offset, int len)
        {
            return serialPort.Read(buf, offset, len);
        }

        public static string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void DiscardInBuffer()
        {
            serialPort.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            serialPort.DiscardOutBuffer();
        }

        public bool IsOpen()
        {
            return serialPort.IsOpen;
        }

        public void Dispose()
        {
            if (IsOpen())
            {
                Close();
            }
        }

    }
    public class UnixSerialDevice : ISerialDevice
    {
        public const int READING_BUFFER_SIZE = 1024;
            
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken CancellationToken => cts.Token;
        private int? fd;
        private readonly IntPtr readingBuffer = Marshal.AllocHGlobal(READING_BUFFER_SIZE);
        protected readonly string portName;
        protected readonly int baudRate;
        protected readonly Parity parity;
        protected readonly int dataBits;
        protected readonly StopBits stopBits;
        public event Action<object, byte[]> DataReceived;

        public static UnixSerialDevice CreateDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            List<string> serial_ports = new List<string>();

            // Are we on Unix?
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new UnixSerialDevice(portName, baudRate, parity, dataBits, stopBits);
            }
            else
            {
                Console.WriteLine("UnixSerialDevice only supports Unix system");
                return null;
            }
        }

        public void Open()
        {
            // open serial port
            int fd = ComWrapper.com_open(portName);

            if (fd == -1)
            {
                throw new Exception($"failed to open port ({portName})");
            }

            ComWrapper.com_set_interface_attribs(fd, baudRate, dataBits, (int)parity, (int)stopBits);
            
            // start reading
            //Task.Run((Action)StartReading, CancellationToken);

            this.fd = fd;
        }

        private UnixSerialDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.portName = portName;
            this.baudRate = baudRate;
            this.parity = parity;
            this.dataBits = dataBits;
            this.stopBits = stopBits;
        }

        private void StartReading()
        {
            if (!fd.HasValue)
            {
                throw new Exception();
            }
            
            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();

                int res = ComWrapper.com_read(fd.Value, readingBuffer, READING_BUFFER_SIZE);
                if (res != -1)
                {
                    byte[] buf = new byte[res];
                    Marshal.Copy(readingBuffer, buf, 0, res);
                    OnDataReceived(buf);
                }

                Thread.Sleep(50);
            }
        }

        protected virtual void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, data);
        }
        
        public void Close()
        {
            if (!fd.HasValue)
            {
                throw new Exception();
            }
            cts.Cancel();
            ComWrapper.com_close(fd.Value);
            Marshal.FreeHGlobal(readingBuffer);
        }
        
        public void Write(byte[] buf, int offset, int len)
        {
            if (!fd.HasValue)
            {
                throw new Exception();
            }
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.Copy(buf, offset, ptr, len);
            ComWrapper.com_write(fd.Value, ptr, len);
            Marshal.FreeHGlobal(ptr);
        }

        public int Read(byte[] buf, int offset, int len)
        {
            if (!fd.HasValue || len > READING_BUFFER_SIZE)
            {
                throw new Exception();
            }
            
            int res = ComWrapper.com_read(fd.Value, readingBuffer, len);
            if (res != -1)
            {
                Marshal.Copy(readingBuffer, buf, offset, res);
            }
            else
            {
                throw new Exception();
            }

            return res;
        }

        public static string[] GetPortNames()
        {
            PlatformID p = Environment.OSVersion.Platform;
            List<string> serial_ports = new List<string>();

            // Are we on Unix?
            if (p == PlatformID.Unix || p == PlatformID.MacOSX)
            {
                string[] ttys = System.IO.Directory.GetFiles("/dev/", "tty*");
                foreach (string dev in ttys)
                {
                    //Arduino MEGAs show up as ttyACM due to their different USB<->RS232 chips
                    if (dev.StartsWith("/dev/ttyS") || dev.StartsWith("/dev/ttyUSB") || dev.StartsWith("/dev/ttyACM") || dev.StartsWith("/dev/ttyAMA"))
                    {
                        serial_ports.Add(dev);
                        //Console.WriteLine("Serial list: {0}", dev);
                    }
                }
            }
            return serial_ports.ToArray();
        }

        public void DiscardInBuffer()
        {
            if (!fd.HasValue)
            {
                throw new Exception();
            }
            ComWrapper.com_tciflush(fd.Value);
        }

        public void DiscardOutBuffer()
        {
            if (!fd.HasValue)
            {
                throw new Exception();
            }
            ComWrapper.com_tcoflush(fd.Value);
        }

        public bool IsOpen()
        {
            if (!fd.HasValue)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Dispose()
        {
            if (IsOpen())
            {
                Close();
            }
        }
    }
}