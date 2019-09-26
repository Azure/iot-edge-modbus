namespace AzureIoTEdgeModbus.DeviceTwin
{
    using AzureIoTEdgeModbus.Configuration;
    using AzureIoTEdgeModbus.Instrumentation;
    using AzureIoTEdgeModbus.Wrappers;

    using Newtonsoft.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class DeviceTwinConfiguration<T> : DeviceConfiguration<T>
    {
        private IModuleClient IotHubModuleClient { get; }

        public DeviceTwinConfiguration(MicrosoftExtensionsLog log, IDeviceConfiguration<T> deviceConfiguration, IModuleClient moduleClient)
            : base(log, deviceConfiguration)
        {
            this.IotHubModuleClient = moduleClient;
        }

        protected override async Task<string> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            // Get desired properties from twin.
            var twin = await this.IotHubModuleClient.GetTwinAsync(cancellationToken).ConfigureAwait(false);

            this.Log.DesiredPropertiesReceivedFromTwin(twin.Properties.Desired);

            return JsonConvert.SerializeObject(twin.Properties.Desired);
        }
    }
}