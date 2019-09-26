namespace AzureIoTEdgeModbus.SerialDevice
{
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
}