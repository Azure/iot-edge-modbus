namespace AzureIoTEdgeModbus
{
    using Configuration;
    using Instrumentation;
    using Slave;
    using Wrappers;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ModbusModule : IEdgeModule
    {
        private bool disposed = false;
        private bool sessionsRun = true;
        private Task startTask;

        private MicrosoftExtensionsLog Log { get; }
        private IModuleClient IotHubModuleClient { get; }
        private IDeviceConfiguration<ModuleConfig> DeviceConfiguration { get; }

        public ModbusModule(MicrosoftExtensionsLog log, IModuleClient moduleClient, IDeviceConfiguration<ModuleConfig> deviceConfiguration)
        {
            this.Log = log;
            this.IotHubModuleClient = moduleClient;
            this.DeviceConfiguration = deviceConfiguration;
        }

        public async Task OpenConnectionAsync(ISessionsHandle sessionsHandle, CancellationToken cancellationToken)
        {
            // Open a connection to the Edge.
            await this.IotHubModuleClient.OpenAsync().ConfigureAwait(false);
            this.Log.IoTHubConnectionOpened();

            // Get desired properties from the configured sources.
            var config = await this.DeviceConfiguration.GetDeviceConfigurationAsync(cancellationToken).ConfigureAwait(false);

            // Callback to receive any changes in twin properties from Iot Hub.
            await this.IotHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(this.OnDesiredPropertiesUpdate,
                sessionsHandle, cancellationToken).ConfigureAwait(false);

            // Open connections to slaves.
            this.startTask = this.StartAsync(await sessionsHandle.CreateHandleFromConfiguration(config).ConfigureAwait(false), config.PublishInterval);
        }

        /// <summary>
        /// Callback to handle Twin desired properties updates.
        /// </summary>
        private async Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            this.Log.DesiredPropertiesReceivedFromTwin(desiredProperties);

            // Drain out any existing sessions before using new twin config.
            this.sessionsRun = false;
            await this.startTask.ConfigureAwait(false);
            this.sessionsRun = true;

            var config = this.DeviceConfiguration.DeserialiseDesiredProperties(JsonConvert.SerializeObject(desiredProperties));

            this.startTask = this.StartAsync(await ((SessionsHandle)userContext).CreateHandleFromConfiguration(config).ConfigureAwait(false), config.PublishInterval);
        }

        private async Task StartAsync(SessionsHandle sessionsHandle, int publishInterval)
        {
            this.Log.ModbusSessionCount(sessionsHandle.modbusSessionList.Count);

            foreach (var session in sessionsHandle.modbusSessionList)
            {
                session.ProcessOperations();
            }

            while (this.sessionsRun)
            {
                var result = await sessionsHandle.CollectAndResetOutMessageFromSessionsAsync().ConfigureAwait(false);

                if (result.Count > 0)
                {
                    var outMessage = new ModbusOutMessage
                    {
                        PublishTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        Content = result
                    };

                    var message = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(outMessage)));
                    message.Properties.Add("content-type", "application/edge-modbus-json");

                    await this.IotHubModuleClient.SendEventAsync("modbusOutput", message).ConfigureAwait(false);
                }

                await Task.Delay(publishInterval).ConfigureAwait(false);
            }

            await sessionsHandle.ReleaseAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.IotHubModuleClient.Dispose();
            }

            this.disposed = true;
        }

        ~ModbusModule()
        {
            this.Dispose(false);
        }
    }
}