namespace spyserv_services.Core.Dtos
{
    public class CpuResultDto
    {
        public string Type { get; } = ResMonType.Cpu.ToString();
        public double UsagePercent { get; set; }
    }
}