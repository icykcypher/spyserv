namespace DataService.Model.MonitoringModel
{
    public class CpuResultDto
    {
        public string Type { get; } = ResMonType.Cpu.ToString();
        public double UsagePercent { get; set; }
    }
}