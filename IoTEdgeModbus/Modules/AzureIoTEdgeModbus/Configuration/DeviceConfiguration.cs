namespace AzureIoTEdgeModbus.Configuration
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using Newtonsoft.Json.Schema.Generation;

    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class DeviceConfiguration<T> : IDeviceConfiguration<T>
    {
        protected ILogger Logger { get; }
        private IDeviceConfiguration<T> Configuration { get; }

        public DeviceConfiguration(ILogger<ModbusModule> logger, IDeviceConfiguration<T> deviceConfiguration)
        {
            this.Logger = logger;
            this.Configuration = deviceConfiguration;
        }

        public DeviceConfiguration(ILogger<ModbusModule> logger)
        {
            this.Logger = logger;
        }

        public async Task<T> GetDeviceConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.Logger.LogInformation($"Attempting to retrieve desired configuration from type {this.GetType().Name}.");
                return this.DeserialiseDesiredProperties(await this.GetConfigurationAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning($"Could not retrieve desired properties from store, error: {ex.Message}");

                return this.Configuration == null ? throw new ConfigurationErrorsException("Could not find settings of the next configuration store.")
                    : await this.Configuration.GetDeviceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public T DeserialiseDesiredProperties(string desiredProperties)
        {
            // Validate Json configuration before deserialising.
            this.ValidateDeviceConfiguration(desiredProperties);

            return JsonConvert.DeserializeObject<T>(desiredProperties);
        }

        protected abstract Task<string> GetConfigurationAsync(CancellationToken cancellationToken);

        private void ValidateDeviceConfiguration(string jsonString)
        {
            var schema = new JSchemaGenerator().Generate(typeof(T));
            var config = JObject.Parse(jsonString);

            if (!config.IsValid(schema, out IList<string> messages))
            {
                this.Logger.LogWarning($"Configuration received is invalid, validation errors- {Environment.NewLine}");
                foreach (var message in messages)
                {
                    this.Logger.LogWarning(message);
                }

                throw new ConfigurationErrorsException(string.Concat(messages));
            }
        }
    }
}