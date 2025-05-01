using MonitoringService.Dto;

namespace MonitoringService.Services
{
    public interface IMonitoringDataService
    {
        Task UpdateDataAsync(MonitoringData data);
        Task<MonitoringData?> GetLatestAsync();
    }
}