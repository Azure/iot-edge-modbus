namespace AzureIoTEdgeModbus.Configuration
{
    using AzureIoTEdgeModbus.Instrumentation;
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
        protected MicrosoftExtensionsLog Log { get; }
        private IDeviceConfiguration<T> Configuration { get; }

        public DeviceConfiguration(MicrosoftExtensionsLog log, IDeviceConfiguration<T> deviceConfiguration)
        {
            this.Log = log;
            this.Configuration = deviceConfiguration;
        }

        public DeviceConfiguration(MicrosoftExtensionsLog log)
        {
            this.Log = log;
        }

        public async Task<T> GetDeviceConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.Log.RetrieveDesiredConfigurationFrom(this.GetType().Name);
                return this.DeserialiseDesiredProperties(await this.GetConfigurationAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                this.Log.ConfgurationRetrivalError(ex);

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
                foreach (var message in messages)
                {
                    this.Log.ConfgurationValidationError(message);
                }

                throw new ConfigurationErrorsException(string.Concat(messages));
            }
        }
    }
}