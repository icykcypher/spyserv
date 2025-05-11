namespace DataService.Model.MonitoringModel
{
    public class MonitoringAppStatusDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;

        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }

        public DateTime LastStarted { get; set; }
    }
}