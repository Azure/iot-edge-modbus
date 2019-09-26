namespace AzureIoTEdgeModbus
{
    using AzureIoTEdgeModbus.Slave;

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEdgeModule : IDisposable
    {
        Task OpenConnectionAsync(ISessionsHandle sessionsHandle, CancellationToken cancellationToken);
    }
}