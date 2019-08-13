namespace AzureIoTEdgeModbus.Configuration
{
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
        public Task<T> GetDeviceConfigurationAsync(CancellationToken cancellationToken)
        {
            return this.GetConfigurationAsync(cancellationToken);
        }

        public abstract Task<T> GetConfigurationAsync(CancellationToken cancellationToken);

        private void ValidateDeviceConfiguration(string jsonString)
        {
            var schema = new JSchemaGenerator().Generate(typeof(T));
            var config = JObject.Parse(jsonString);

            if (!config.IsValid(schema, out IList<string> messages))
            {
                Console.WriteLine($"Configuration received is invalid, validation errors- {Environment.NewLine}");
                foreach (var message in messages)
                {
                    Console.WriteLine(message);
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