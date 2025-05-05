namespace spyserv_services.Core.Dtos
{
    public class MonitoringData
    {
        public CpuResultDto? CpuResult { get; set; }
        public MemoryResultDto? MemoryResult { get; set; }
        public DiskResultDto? DiskResult { get; set; }
    }
}