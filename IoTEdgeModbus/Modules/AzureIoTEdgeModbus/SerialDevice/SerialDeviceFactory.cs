namespace AzureIoTEdgeModbus.SerialDevice
{
    using System.IO.Ports;
    using System.Runtime.InteropServices;

    public static class SerialDeviceFactory
    {
        public static ISerialDevice CreateSerialDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            ISerialDevice device = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                device = WinSerialDevice.CreateDevice(portName, baudRate, parity, dataBits, stopBits);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                device = UnixSerialDevice.CreateDevice(portName, baudRate, parity, dataBits, stopBits);
            }
            return device;
        }
    }
}