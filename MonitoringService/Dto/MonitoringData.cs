namespace MonitoringService.Dto
{
    public class MonitoringData
    {
        public string UserEmail { get; set; } = string.Empty;
        public CpuResultDto? CpuResult { get; set; }
        public MemoryResultDto? MemoryResult { get; set; }
        public DiskResultDto? DiskResult { get; set; }
    }
}