using MonitoringService.Dto;

namespace MonitoringService.Services
{
    public interface IMonitoringDataService
    {
        Task UpdateDataAsync(string userEmail, string deviceName, Dto.MonitoringData data);
        Task<MonitoringData?> GetLatestAsync(string userEmail, string deviceName);
    }
}