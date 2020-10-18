﻿using System;
using System.IO.Ports;

namespace tempSerialPort
{
    public class SerialDevice : IDisposable
    {
        private SerialPort serialPort = null;

        public SerialDevice(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            this.serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        public void Open()
        {
            this.serialPort.Open();
            this.serialPort.Handshake = Handshake.None;
            this.serialPort.ReadTimeout = 5000;
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
}