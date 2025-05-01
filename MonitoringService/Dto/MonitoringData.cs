namespace MonitoringService.Dto
{
    public class MonitoringData
    {
        public CpuResultDto? CpuResult { get; set; }
        public MemoryResultDto? MemoryResult { get; set; }
        public DiskResultDto? DiskResult { get; set; }
    }
}