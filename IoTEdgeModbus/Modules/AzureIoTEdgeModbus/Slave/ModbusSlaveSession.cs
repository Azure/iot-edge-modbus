namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data;
    using Decoding;

    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    public abstract class ModbusSlaveSession
    {
        protected const int BufferSize = 512;
        protected const int ModbusExceptionCode = 0x80;

        public ModbusSlaveConfig Config;

        protected ModbusOutContent OutMessage = null;
        protected SemaphoreSlim SemaphoreCollection = new SemaphoreSlim(1, 1);
        protected SemaphoreSlim SemaphoreConnection = new SemaphoreSlim(1, 1);
        protected bool IsRunning = false;

        private List<Task> taskList = new List<Task>();

        protected abstract int RequestSize { get; }
        protected abstract int FunctionCodeOffset { get; }
        protected abstract int Silent { get; }

        protected int ByteCountOffset => this.FunctionCodeOffset + 1;
        protected int DataValuesOffset => this.FunctionCodeOffset + 2;


        protected ModbusSlaveSession(ModbusSlaveConfig conf)
        {
            this.Config = conf;
        }

        public abstract void ReleaseSession();

        public async Task InitSession()
        {
            await this.ConnectSlave();

            foreach (var operation in this.Config.Operations.Values)
            {
                operation.RequestLen = this.RequestSize;
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
            this.IsRunning = true;
            foreach (var op_pair in this.Config.Operations)
            {
                ReadOperation x = op_pair.Value;
                Task t = Task.Run(async () => await this.SingleOperation(x));
                this.taskList.Add(t);
            }
        }
        public ModbusOutContent GetOutMessage()
        {
            return this.OutMessage;
        }
        public void ClearOutMessage()
        {
            this.SemaphoreCollection.Wait();

            this.OutMessage = null;

            this.SemaphoreCollection.Release();
        }

        protected abstract void EncodeWrite(byte[] writeRequest, WriteOperation readOperation);
        protected abstract Task<byte[]> SendRequest(byte[] request, int reqLen);
        protected abstract Task ConnectSlave();
        protected abstract void EncodeRead(ReadOperation operation);
        protected async Task SingleOperation(ReadOperation operation)
        {
            while (this.IsRunning)
            {
                operation.Response = null;
                operation.Response = await this.SendRequest(operation.Request, operation.RequestLen);

                if (operation.Response != null)
                {
                    if (operation.Request[this.DataValuesOffset] == operation.Response[this.DataValuesOffset])
                    {
                        this.ProcessResponse(this.Config, operation);
                    }
                    else if (operation.Request[this.DataValuesOffset] + ModbusExceptionCode == operation.Response[this.DataValuesOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {operation.Response[this.DataValuesOffset + 1]}");
                    }
                }

                await Task.Delay(operation.PollingInterval - this.Silent);
            }
        }

        public List<ModbusOutValue> ProcessResponse(ModbusSlaveConfig config, ReadOperation operation)
        {
            var response = new Span<byte>(operation.Response);
            int byteCount = response[this.ByteCountOffset];
            var dataBytes = response.Slice(this.DataValuesOffset, operation.Count);

            // TODO: Fix addresses in result
            var values = operation.Decoder.GetValues(dataBytes, byteCount)
                .Select(v => new ModbusOutValue() { DisplayName = operation.DisplayName, Address = "TODO", Value = v }).ToList();

            if (values.Count > 0)
            {
                this.PrepareOutMessage(config, operation.CorrelationId, values);
            }

            return values;
        }

        private void PrepareOutMessage(ModbusSlaveConfig config, string correlationId, List<ModbusOutValue> valueList)
        {  
            this.SemaphoreCollection.Wait();
            ModbusOutContent content = null;
            if (this.OutMessage == null)
            {
                content = new ModbusOutContent
                {
                    HwId = config.HwId,
                    Data = new List<ModbusOutData>(),
                    AdditionalProperties = config.AdditionalProperties
                };
                this.OutMessage = content;
            }
            else
            {
                content = this.OutMessage;
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

            this.SemaphoreCollection.Release();

        }
        protected void ReleaseOperations()
        {
            this.IsRunning = false;
            Task.WaitAll(this.taskList.ToArray());
            this.taskList.Clear();
        }
    }
}
