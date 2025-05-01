using MonitoringService.Dto;

namespace MonitoringService.Services
{
    public class MonitoringDataService : IMonitoringDataService
    {
        public async Task UpdateDataAsync(MonitoringData data)
        {
            //var current = await _db.MonitoringData.FirstOrDefaultAsync();

            //if (current == null)
            //{
            //    _db.MonitoringData.Add(Map(data));
            //}
            //else
            //{
            //    current.CpuUsage = data.CpuResult?.UsagePercent ?? 0;
            //    current.MemoryUsed = data.MemoryResult?.UsedPercent ?? 0;
            //    current.MemoryTotal = data.MemoryResult?.TotalMemoryMb ?? 0;
            //    current.DiskDevice = data.DiskResult?.Device ?? string.Empty;
            //    current.DiskRead = data.DiskResult?.ReadMbps ?? 0;
            //    current.DiskWrite = data.DiskResult?.WriteMbps ?? 0;
            //    current.UpdatedAt = DateTime.UtcNow;
            //}

            //await _db.SaveChangesAsync();
        }

        public async Task<MonitoringData?> GetLatestAsync()
        {
            //var current = await _db.MonitoringData.FirstOrDefaultAsync();
            //return MapToDto(current);

            return null;
        }
    }
}