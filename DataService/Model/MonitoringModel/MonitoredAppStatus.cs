namespace DataService.Model.MonitoringModel
{
    public class MonitoredAppStatus
    {
        public Guid Id { get; set; }
        public Guid MonitoredAppId { get; set; }
        public virtual MonitoredApp MonitoredApp { get; set; } = null!;
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public DateTime LastStarted { get; set; }
        public bool IsRunning { get; set; }
        public DateTime Timestamp { get; set; }
    }
}