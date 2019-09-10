namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data;
    using Decoding;
    using DotNetty.Common.Utilities;

    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    public abstract class ModbusSlaveSession
    {
        protected const int BufferSize = 512;
        protected const int ModbusExceptionCode = 0x80;

        protected readonly ModbusSlaveConfig config;

        private ModbusOutContent outMessage = null;
        private readonly SemaphoreSlim semaphoreCollection = new SemaphoreSlim(1, 1);
        protected readonly SemaphoreSlim semaphoreConnection = new SemaphoreSlim(1, 1);
        private bool isRunning = false;

        private readonly List<Task> taskList = new List<Task>();

        protected abstract int RequestSize { get; }
        protected abstract int FunctionCodeOffset { get; }
        protected abstract int Silent { get; }

        private int ByteCountOffset => this.FunctionCodeOffset + 1;
        private int DataValuesOffset => this.FunctionCodeOffset + 2;


        protected ModbusSlaveSession(ModbusSlaveConfig conf)
        {
            this.config = conf;
        }

        public abstract void ReleaseSession();

        public async Task InitSession()
        {
            await this.ConnectSlave();

            foreach (var operation in this.config.Operations.Values)
            {
                operation.RequestLength = this.RequestSize;
                operation.Request = new byte[BufferSize];
                operation.Decoder = DataDecoderFactory.CreateDecoder(operation.FunctionCode, operation.DataType);

                this.EncodeRead(operation);
            }
        }

        public async Task WriteMessage(WriteOperation operation)
        {
            if (operation.DataType != ModbusDataType.Int16)
            {
                //Handling complex value
                //Obtain value split into several int values
                var valuesToBeWritten = ModbusComplexValuesHandler.SplitComplexValue(operation.Value, operation.DataType);

                //We need to write each part of complex value to separate registry
                foreach (var v in valuesToBeWritten)
                {
                    operation.IntValueToWrite = Convert.ToUInt16(operation.Value);
                    await WriteCB(operation);
                    operation.StartAddress = (Convert.ToInt32(operation.StartAddress) + 1).ToString();
                }
            }
            else
            {
                operation.IntValueToWrite = Convert.ToUInt16(operation.Value);
                await WriteCB(operation);
            }
        }

        public async Task WriteCB(WriteOperation operation)
        {
            byte[] writeRequest = new byte[BufferSize];
            byte[] writeResponse = null;
            int reqLen = this.RequestSize;

            this.EncodeWrite(writeRequest, operation);

            writeResponse = await this.SendRequest(writeRequest, reqLen);
        }

        public void ProcessOperations()
        {
            this.isRunning = true;
            foreach (var op_pair in this.config.Operations)
            {
                ReadOperation x = op_pair.Value;
                Task t = Task.Run(() => this.SingleOperationAsync(x));
                this.taskList.Add(t);
            }
        }
        public ModbusOutContent GetOutMessage()
        {
            return this.outMessage;
        }
        public void ClearOutMessage()
        {
            this.semaphoreCollection.Wait();

            this.outMessage = null;

            this.semaphoreCollection.Release();
        }

        protected abstract void EncodeWrite(byte[] writeRequest, WriteOperation readOperation);
        protected abstract Task<byte[]> SendRequest(byte[] request, int reqLen);
        protected abstract Task ConnectSlave();
        protected abstract void EncodeRead(ReadOperation operation);

        private async Task SingleOperationAsync(ReadOperation operation)
        {
            while (this.isRunning)
            {
                operation.Response = null;
                operation.Response = await this.SendRequest(operation.Request, operation.RequestLength);

                if (operation.Response != null)
                {
                    if (operation.Request[this.FunctionCodeOffset] == operation.Response[this.FunctionCodeOffset])
                    {
                        var values = this.DecodeResponse(operation).ToList();

                        this.PrepareOutMessage(
                            operation.CorrelationId,
                            values.Select(v => new ModbusOutValue { Address = v.Address.ToString(), DisplayName = operation.DisplayName, Value = v.Value }));
                    }
                    else if (operation.Request[this.FunctionCodeOffset] + ModbusExceptionCode == operation.Response[this.FunctionCodeOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {operation.Response[this.FunctionCodeOffset]}");
                    }
                }

                await Task.Delay(operation.PollingInterval - this.Silent);
            }
        }

        public IEnumerable<DecodedValue> DecodeResponse(ReadOperation operation)
        {
            var response = operation.Response;
            int byteCount = response[this.ByteCountOffset];
            var dataBytes = response.Slice(this.DataValuesOffset, byteCount);

            return operation.Decoder.GetValues(dataBytes, operation);
        }

        private void PrepareOutMessage(string correlationId, IEnumerable<ModbusOutValue> valueList)
        {
            this.semaphoreCollection.Wait();
            ModbusOutContent content;
            if (this.outMessage == null)
            {
                content = new ModbusOutContent
                {
                    HwId = config.HwId,
                    Data = new List<ModbusOutData>(),
                    AdditionalProperties = config.AdditionalProperties
                };
                this.outMessage = content;
            }
            else
            {
                content = this.outMessage;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ModbusOutData data = null;
            foreach (var d in content.Data)
            {
                if (d.CorrelationId == correlationId && d.SourceTimestamp == timestamp)
                {
                    data = d;
                    break;
                }
            }
            if (data == null)
            {
                data = new ModbusOutData
                {
                    CorrelationId = correlationId,
                    SourceTimestamp = timestamp,
                    Values = new List<ModbusOutValue>()
                };
                content.Data.Add(data);
            }

            data.Values.AddRange(valueList);

            this.semaphoreCollection.Release();

        }
        protected void ReleaseOperations()
        {
            this.isRunning = false;
            Task.WaitAll(this.taskList.ToArray());
            this.taskList.Clear();
        }
    }
}
