namespace AzureIoTEdgeModbus.SerialDevice
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Runtime.InteropServices;
    using System.Threading;
    using tempSerialPort;

    public class UnixSerialDevice : ISerialDevice
    {
        public const int READING_BUFFER_SIZE = 1024;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken CancellationToken => this.cts.Token;
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
            int fd = ComWrapper.com_open(this.portName);

            if (fd == -1)
            {
                throw new Exception($"failed to open port ({this.portName})");
            }

            ComWrapper.com_set_interface_attribs(fd, this.baudRate, this.dataBits, (int)this.parity, (int)this.stopBits);

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
            if (!this.fd.HasValue)
            {
                throw new Exception();
            }

            while (true)
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                int res = ComWrapper.com_read(this.fd.Value, this.readingBuffer, READING_BUFFER_SIZE);
                if (res != -1)
                {
                    byte[] buf = new byte[res];
                    Marshal.Copy(this.readingBuffer, buf, 0, res);
                    this.OnDataReceived(buf);
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
            if (!this.fd.HasValue)
            {
                throw new Exception();
            }
            this.cts.Cancel();
            ComWrapper.com_close(this.fd.Value);
            Marshal.FreeHGlobal(this.readingBuffer);
        }

        public void Write(byte[] buf, int offset, int len)
        {
            if (!this.fd.HasValue)
            {
                throw new Exception();
            }
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.Copy(buf, offset, ptr, len);
            ComWrapper.com_write(this.fd.Value, ptr, len);
            Marshal.FreeHGlobal(ptr);
        }

        public int Read(byte[] buf, int offset, int len)
        {
            if (!this.fd.HasValue || len > READING_BUFFER_SIZE)
            {
                throw new Exception();
            }

            int res = ComWrapper.com_read(this.fd.Value, this.readingBuffer, len);
            if (res != -1)
            {
                Marshal.Copy(this.readingBuffer, buf, offset, res);
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
            if (!this.fd.HasValue)
            {
                throw new Exception();
            }
            ComWrapper.com_tciflush(this.fd.Value);
        }

        public void DiscardOutBuffer()
        {
            if (!this.fd.HasValue)
            {
                throw new Exception();
            }
            ComWrapper.com_tcoflush(this.fd.Value);
        }

        public bool IsOpen()
        {
            if (!this.fd.HasValue)
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
            if (this.IsOpen())
            {
                this.Close();
            }
        }
    }
}
