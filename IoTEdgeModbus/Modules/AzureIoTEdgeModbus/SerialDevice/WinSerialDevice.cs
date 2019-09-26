namespace AzureIoTEdgeModbus.SerialDevice
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Runtime.InteropServices;

    public class WinSerialDevice : ISerialDevice
    {
        private readonly SerialPort serialPort = null;
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
            this.serialPort.ReadTimeout = 5000;
        }

        private WinSerialDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        public void Close()
        {
            this.serialPort.Close();
        }

        public void Write(byte[] buf, int offset, int len)
        {
            this.serialPort.Write(buf, offset, len);
        }

        public int Read(byte[] buf, int offset, int len)
        {
            return this.serialPort.Read(buf, offset, len);
        }

        public static string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void DiscardInBuffer()
        {
            this.serialPort.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            this.serialPort.DiscardOutBuffer();
        }

        public bool IsOpen()
        {
            return this.serialPort.IsOpen;
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
