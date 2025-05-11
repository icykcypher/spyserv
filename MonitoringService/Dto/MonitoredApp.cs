namespace MonitoringService.Dto
{
    public class MonitoredApp
    {
        public Guid Id { get; set; }
        public Guid ClientAppId { get; set; }
        public virtual ClientApp? ClientApp { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRunning { get; set; } = true;

        public virtual ICollection<MonitoredAppStatus> StatusHistory { get; set; } = new List<MonitoredAppStatus>();
    }
}