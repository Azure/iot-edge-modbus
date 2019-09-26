namespace AzureIoTEdgeModbus.Wrappers
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IModuleClient : IDisposable
    {
        Task OpenAsync();
        Task SendEventAsync(string outputName, Message message);
        Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler, object userContext, CancellationToken cancellationToken);
        Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext, CancellationToken cancellationToken);
        Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken);
        Task<Twin> GetTwinAsync(CancellationToken cancellationToken);
    }
}