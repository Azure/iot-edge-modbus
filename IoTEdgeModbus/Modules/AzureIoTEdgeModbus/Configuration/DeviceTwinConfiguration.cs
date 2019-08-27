namespace AzureIoTEdgeModbus.DeviceTwin
{
    using AzureIoTEdgeModbus.Configuration;
    using AzureIoTEdgeModbus.Wrappers;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class DeviceTwinConfiguration<T> : DeviceConfiguration<T>
    {
        private IModuleClient IotHubModuleClient { get; }

        public DeviceTwinConfiguration(ILogger<ModbusModule> logger, IDeviceConfiguration<T> deviceConfiguration, IModuleClient moduleClient)
            : base(logger, deviceConfiguration)
        {
            this.IotHubModuleClient = moduleClient;
        }

        protected override async Task<string> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            // Get desired properties from twin.
            var twin = await this.IotHubModuleClient.GetTwinAsync(cancellationToken).ConfigureAwait(false);
            this.Logger.LogInformation($"Desired properties retrieved from twin: {Environment.NewLine}{twin.Properties.Desired}");

            return JsonConvert.SerializeObject(twin.Properties.Desired);
        }
    }
}