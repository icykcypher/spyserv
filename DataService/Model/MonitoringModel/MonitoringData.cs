namespace DataService.Model.MonitoringModel
{
    public class MonitoringData
    {
        public Guid Id { get; set; }
        public virtual ClientApp ClientApp { get; set; } = null!;
        public CpuResultDto? CpuResult { get; set; }
        public MemoryResultDto? MemoryResult { get; set; }
        public DiskResultDto? DiskResult { get; set; }
        public DateTime Timestamp { get; set; }
    }
}