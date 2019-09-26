namespace AzureIoTEdgeModbus.Configuration
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IDeviceConfiguration<T>
    {
        Task<T> GetDeviceConfigurationAsync(CancellationToken cancellationToken);
        T DeserialiseDesiredProperties(string desiredProperties);
    }
}