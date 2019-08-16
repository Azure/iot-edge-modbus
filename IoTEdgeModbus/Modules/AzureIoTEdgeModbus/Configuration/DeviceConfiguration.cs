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

        public DeviceConfiguration(ILogger<ModbusModule> logger)
        {
            this.Logger = logger;
        }

        public Task<T> GetDeviceConfigurationAsync(CancellationToken cancellationToken)
        {
            return this.GetConfigurationAsync(cancellationToken);
        }

        protected abstract Task<T> GetConfigurationAsync(CancellationToken cancellationToken);

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

        public T DeserialiseDesiredProperties(string desiredProperties)
        {
            // Validate Json configuration before deserialising.
            this.ValidateDeviceConfiguration(desiredProperties);

            return JsonConvert.DeserializeObject<T>(desiredProperties);
        }
    }
}