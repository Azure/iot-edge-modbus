namespace AzureIoTEdgeModbus.Slave
{
    using System.Threading.Tasks;

    public interface ISessionsHandle
    {
        Task<SessionsHandle> CreateHandleFromConfiguration(ModuleConfig config);
    }
}