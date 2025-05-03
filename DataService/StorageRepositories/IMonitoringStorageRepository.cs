using DataService.Model.MonitoringModel;

namespace DataService.StorageRepositories
{
    public interface IMonitoringStorageRepository
    {
        Task<ClientApp> AddNewClientAppAsync(ClientApp clientApp);
    }
}