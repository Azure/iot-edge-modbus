using AzureIoTEdgeModbus.Slave;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.IoT.Edge.Modbus.Tests
{
    public class ExposedModbusTCPSlaveSession : ModbusSlaveSession
    {
        protected override int m_dataBodyOffset => 7;

        public ExposedModbusTCPSlaveSession(ModbusSlaveConfig conf)
          : base(conf)
        {
        }

        public List<ModbusOutValue> ProcessResponse(ModbusSlaveConfig config, ReadOperation x)
        {
            return base.ProcessResponse(config,x);
        }

        public override void ReleaseSession()
        {
            throw new System.NotImplementedException();
        }

        protected override Task ConnectSlave()
        {
            //Bypassing connecting to Slave
            return Task.FromResult<object>(null);
        }

        protected override void EncodeRead(ReadOperation operation)
        {
            //We don't need this as ExposedModbusTCPSlaveSession is used to test response processing functionality
            return;
        }

        protected override void EncodeWrite(byte[] request, WriteOperation readOperation)
        {
            throw new System.NotImplementedException();
        }

        protected override Task<byte[]> SendRequest(byte[] request, int reqLen)
        {
            throw new System.NotImplementedException();
        }
    }
}
