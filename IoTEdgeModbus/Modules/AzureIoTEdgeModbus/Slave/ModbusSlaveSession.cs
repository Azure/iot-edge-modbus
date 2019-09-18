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

        public abstract Task ReleaseSessionAsync();

        public async Task InitSessionAsync()
        {
            await this.ConnectSlaveAsync().ConfigureAwait(false);

            foreach (var operation in this.config.Operations.Values)
            {
                operation.RequestLength = this.RequestSize;
                operation.Request = new byte[this.RequestSize];
                operation.Decoder = DataDecoderFactory.CreateDecoder(operation.FunctionCode, operation.DataType);

                this.EncodeRead(operation);
            }
        }

        public async Task WriteMessageAsync(WriteOperation operation)
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
                    await this.WriteCBAsync(operation).ConfigureAwait(false);
                    operation.StartAddress = (Convert.ToInt32(operation.StartAddress) + 1).ToString();
                }
            }
            else
            {
                operation.IntValueToWrite = Convert.ToUInt16(operation.Value);
                await this.WriteCBAsync(operation).ConfigureAwait(false);
            }
        }

        private async Task WriteCBAsync(WriteOperation operation)
        {
            byte[] writeRequest = new byte[this.RequestSize];

            this.EncodeWrite(writeRequest, operation);

            await this.SendRequestAsync(writeRequest).ConfigureAwait(false);
        }

        public void ProcessOperations()
        {
            this.isRunning = true;
            foreach (var operation in this.config.Operations.Values)
            {
                var task = Task.Run(() => this.SingleOperationAsync(operation));
                this.taskList.Add(task);
            }
        }
        public ModbusOutContent GetOutMessage()
        {
            return this.outMessage;
        }
        public async Task ClearOutMessageAsync()
        {
            await this.semaphoreCollection.WaitAsync().ConfigureAwait(false);

            this.outMessage = null;

            this.semaphoreCollection.Release();
        }

        protected abstract void EncodeWrite(byte[] writeRequest, WriteOperation readOperation);
        protected abstract Task<byte[]> SendRequestAsync(byte[] request);
        protected abstract Task ConnectSlaveAsync();
        protected abstract void EncodeRead(ReadOperation operation);

        private async Task SingleOperationAsync(ReadOperation operation)
        {
            while (this.isRunning)
            {
                operation.Response = null;
                operation.Response = await this.SendRequestAsync(operation.Request).ConfigureAwait(false);

                if (operation.Response != null)
                {
                    if (operation.Request[this.FunctionCodeOffset] == operation.Response[this.FunctionCodeOffset])
                    {
                        var values = this.DecodeResponse(operation).ToList();

                        await this.PrepareOutMessageAsync(
                            operation.CorrelationId,
                            values.Select(v => new ModbusOutValue { Address = v.Address.ToString(), DisplayName = operation.DisplayName, Value = v.Value }))
                            .ConfigureAwait(false);
                    }
                    else if (operation.Request[this.FunctionCodeOffset] + ModbusExceptionCode == operation.Response[this.FunctionCodeOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {operation.Response[this.FunctionCodeOffset]}");
                    }
                }

                await Task.Delay(operation.PollingInterval - this.Silent).ConfigureAwait(false);
            }
        }

        public IEnumerable<DecodedValue> DecodeResponse(ReadOperation operation)
        {
            var response = operation.Response;
            int byteCount = response[this.ByteCountOffset];
            var dataBytes = response.Slice(this.DataValuesOffset, byteCount);

            return operation.Decoder.GetValues(dataBytes, operation);
        }

        private async Task PrepareOutMessageAsync(string correlationId, IEnumerable<ModbusOutValue> valueList)
        {
            await this.semaphoreCollection.WaitAsync().ConfigureAwait(false);
            ModbusOutContent content;
            if (this.outMessage == null)
            {
                content = new ModbusOutContent
                {
                    HwId = this.config.HwId,
                    Data = new List<ModbusOutData>(),
                    AdditionalProperties = this.config.AdditionalProperties
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
        protected async Task ReleaseOperationsAsync()
        {
            this.isRunning = false;
            await Task.WhenAll(this.taskList.ToArray()).ConfigureAwait(false);
            this.taskList.Clear();
        }
    }
}
