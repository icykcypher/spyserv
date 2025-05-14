namespace spyserv_services.Core.Dtos
{
    public class RegisterDeviceRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
    }

    public class RegisterDeviceResponse
    {
        public string AuthToken { get; set; } = string.Empty;
    }

    public class MonitoringDataRequest
    {
        public CpuResultDto? CpuResult { get; set; }
        public MemoryResultDto? MemoryResult { get; set; }
        public DiskResultDto? DiskResult { get; set; }
    }

    public class AppStatusDto
    {
        public string AppName { get; set; } = string.Empty;
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public bool IsRunning { get; set; }
        public DateTime LastChecked { get; set; }
    }

    public class AppStatusesRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public List<AppStatusDto> Statuses { get; set; } = new();
    }
}