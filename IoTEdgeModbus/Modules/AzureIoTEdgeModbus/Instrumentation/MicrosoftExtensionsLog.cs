namespace AzureIoTEdgeModbus.Instrumentation
{
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Reflection;

    /// <summary>
    /// Conventions Used for Event Ids-
    /// 1xxx- Trace Events
    /// 2xxx- Informational Events
    /// 3xxx- Warning Events
    /// 4xxx- Error Events
    /// </summary>
    public class MicrosoftExtensionsLog : EventLog
    {
        private readonly ILogger logger;

        public MicrosoftExtensionsLog(ILogger logger)
        {
            this.logger = logger;
        }

        [Event(1000, Description = "Modbus sessions available: {0}")]
        public void ModbusSessionCount(int sessionCount)
        {
            this.LogTrace(MethodBase.GetCurrentMethod(), sessionCount);
        }

        [Event(2000, Description = "Connection to remote IoT Hub opened.")]
        public void IoTHubConnectionOpened()
        {
            this.LogInformation(MethodBase.GetCurrentMethod());
        }
        
        [Event(2002, Description = "New desired properties received: {0}.")]
        public void DesiredPropertiesReceivedFromTwin(TwinCollection twinCollection)
        {
            this.LogInformation(MethodBase.GetCurrentMethod(), twinCollection);
        }

        [Event(2003, Description = "New desired properties received: {0}.")]
        public void DesiredPropertiesReceivedFromFile(string desiredProperties)
        {
            this.LogInformation(MethodBase.GetCurrentMethod(), desiredProperties);
        }

        [Event(2004, Description = "Attempting to retrieve desired configuration from provider: {0}.")]
        public void RetrieveDesiredConfigurationFrom(string configurationProviderTypeName)
        {
            this.LogInformation(MethodBase.GetCurrentMethod(), configurationProviderTypeName);
        }

        [Event(3000, Description = "Configuration received is invalid, JSON schema validation error: {0}.")]
        public void ConfgurationValidationError(string errorMessage)
        {
            this.LogWarning(MethodBase.GetCurrentMethod(), errorMessage);
        }

        [Event(4000, Description = "Could not retrieve desired properties from store, error: {0}.")]
        public void ConfgurationRetrivalError(Exception exception)
        {
            this.LogError(MethodBase.GetCurrentMethod(), exception);
        }

        protected override void LogTrace(int id, string name, string description, params object[] args)
        {
            this.logger.LogTrace(new EventId(id, name), description, args);
        }

        protected override void LogInformation(int id, string name, string description, params object[] args)
        {
            this.logger.LogInformation(new EventId(id, name), description, args);
        }

        protected override void LogWarning(int id, string name, string description, params object[] args)
        {
            this.logger.LogWarning(new EventId(id, name), description, args);
        }

        protected override void LogError(int id, string name, Exception exception, string description, params object[] args)
        {
            this.logger.LogError(new EventId(id, name), exception, description, args);
        }
    }
}