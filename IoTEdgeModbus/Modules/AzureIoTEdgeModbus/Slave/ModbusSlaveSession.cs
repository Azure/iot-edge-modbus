namespace AzureIoTEdgeModbus.Slave
{
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    public abstract class ModbusSlaveSession
    {
        public static int ModbusExceptionCode = 0x80;

        public ModbusSlaveConfig config;
        protected ModbusOutContent OutMessage = null;
        protected const int m_bufSize = 512;
        protected SemaphoreSlim m_semaphore_collection = new SemaphoreSlim(1, 1);
        protected SemaphoreSlim m_semaphore_connection = new SemaphoreSlim(1, 1);
        protected bool m_run = false;
        protected List<Task> m_taskList = new List<Task>();
        protected virtual int m_reqSize { get; }
        protected virtual int m_dataBodyOffset { get; }
        protected virtual int m_silent { get; }

        #region Constructors
        public ModbusSlaveSession(ModbusSlaveConfig conf)
        {
            this.config = conf;
        }
        #endregion

        #region Public Methods
        public abstract void ReleaseSession();

        public async Task InitSession()
        {
            await this.ConnectSlave();

            foreach (var op_pair in this.config.Operations)
            {
                ReadOperation x = op_pair.Value;

                //Can't read float from coils/inputs
                if (x.FunctionCode == (byte)FunctionCodeType.ReadCoils || x.FunctionCode == (byte)FunctionCodeType.ReadInputs)
                    x.ValueType = ModbusValueType.Basic;

                //If working with complex value we need to override count
                if (x.ValueType != ModbusValueType.Basic)
                    x.Count = ModbusComplexValuesHandler.GetCountForValueType(x.ValueType);
                  
              
                x.RequestLen = this.m_reqSize;
                x.Request = new byte[m_bufSize];

                this.EncodeRead(x);
            }
        }

        public async Task WriteMessage(WriteOperation operation)
        {
            if (operation.ValueType != ModbusValueType.Basic)
            {
                //Handling complex value
                //Obtain value split into several int values
                var valuesToBeWritten = ModbusComplexValuesHandler.SplitComplexValue(operation.Value, operation.ValueType);

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
            byte[] writeRequest = new byte[m_bufSize];
            byte[] writeResponse = null;
            int reqLen = this.m_reqSize;

            this.EncodeWrite(writeRequest, operation);

            writeResponse = await this.SendRequest(writeRequest, reqLen);
        }

        public void ProcessOperations()
        {
            this.m_run = true;
            foreach (var op_pair in this.config.Operations)
            {
                ReadOperation x = op_pair.Value;
                Task t = Task.Run(async () => await this.SingleOperation(x));
                this.m_taskList.Add(t);
            }
        }
        public ModbusOutContent GetOutMessage()
        {
            return this.OutMessage;
        }
        public void ClearOutMessage()
        {
            this.m_semaphore_collection.Wait();

            this.OutMessage = null;

            this.m_semaphore_collection.Release();
        }
        #endregion

        #region Protected Methods
        protected abstract void EncodeWrite(byte[] writeRequest, WriteOperation readOperation);
        protected abstract Task<byte[]> SendRequest(byte[] request, int reqLen);
        protected abstract Task ConnectSlave();
        protected abstract void EncodeRead(ReadOperation operation);
        protected async Task SingleOperation(ReadOperation x)
        {
            while (this.m_run)
            {
                x.Response = null;
                x.Response = await this.SendRequest(x.Request, x.RequestLen);

                string res = JsonConvert.SerializeObject(x);

                if (x.Response != null)
                {
                    if (x.Request[this.m_dataBodyOffset] == x.Response[this.m_dataBodyOffset])
                    {
                        this.ProcessResponse(this.config, x);
                    }
                    else if (x.Request[this.m_dataBodyOffset] + ModbusExceptionCode == x.Response[this.m_dataBodyOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {x.Response[this.m_dataBodyOffset + 1]}");
                    }
                }

                await Task.Delay(x.PollingInterval - this.m_silent);
            }
        }

        protected List<ModbusOutValue> ProcessResponse(ModbusSlaveConfig config, ReadOperation x)
        {
            int count = 0;
            int step_size = 0;
            int start_digit = 0;
            List<ModbusOutValue> value_list = new List<ModbusOutValue>();
            switch (x.Response[m_dataBodyOffset])//function code
            {
                case (byte)FunctionCodeType.ReadCoils:
                case (byte)FunctionCodeType.ReadInputs:
                    {
                        count = x.Response[m_dataBodyOffset + 1] * 8;
                        count = (count > x.Count) ? x.Count : count;
                        step_size = 1;
                        start_digit = x.Response[m_dataBodyOffset] - 1;
                        break;
                    }
                case (byte)FunctionCodeType.ReadHoldingRegisters:
                case (byte)FunctionCodeType.ReadInputRegisters:
                    {
                        count = x.Response[m_dataBodyOffset + 1];
                        step_size = 2;
                        start_digit = (x.Response[m_dataBodyOffset] == 3) ? 4 : 3;
                        break;
                    }
            }
            var initialCell = string.Format(x.OutFormat, (char)x.Entity, x.Address + 1);

            if (x.ValueType == ModbusValueType.Basic)
            {
                for (int i = 0; i < count; i += step_size)
                {
                    string res = "";
                    string cell = "";
                    string val = "";
                    if (step_size == 1)
                    {
                        cell = string.Format(x.OutFormat, (char)x.Entity, x.Address + i + 1);
                        val = string.Format("{0}", (x.Response[m_dataBodyOffset + 2 + (i / 8)] >> (i % 8)) & 0b1);
                    }
                    else if (step_size == 2)
                    {
                        cell = string.Format(x.OutFormat, (char)x.Entity, x.Address + (i / 2) + 1);
                        val = string.Format("{0,00000}", ((x.Response[m_dataBodyOffset + 2 + i]) * 0x100 + x.Response[m_dataBodyOffset + 3 + i]));
                    }
                    res = cell + ": " + val + "\n";
                    Console.WriteLine(res);

                    ModbusOutValue value = new ModbusOutValue()
                    { DisplayName = x.DisplayName, Address = cell, Value = val };
                    value_list.Add(value);
                }
            }
            //We need to merge complex value
            else
            {
                var bytesA = x.Response.SubArray(m_dataBodyOffset + 2, step_size * x.Count);
                var val = ModbusComplexValuesHandler.MergeComplexValue(bytesA,  x.ValueType, this.config.EndianSwap, this.config.MidEndianSwap);

                ModbusOutValue value = new ModbusOutValue()
                { DisplayName = x.DisplayName, Address = initialCell, Value = val };
                value_list.Add(value);

                var res = initialCell + " : " + val + "\n";
                Console.WriteLine(res);
            }

            if (value_list.Count > 0)
            {
                this.PrepareOutMessage(config, x.CorrelationId, value_list);
            }

            return value_list;
        }

        private void PrepareOutMessage(ModbusSlaveConfig config, string correlationId, List<ModbusOutValue> valueList)
        {  
            this.m_semaphore_collection.Wait();
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

            this.m_semaphore_collection.Release();

        }
        protected void ReleaseOperations()
        {
            this.m_run = false;
            Task.WaitAll(this.m_taskList.ToArray());
            this.m_taskList.Clear();
        }
        #endregion
    }
}
