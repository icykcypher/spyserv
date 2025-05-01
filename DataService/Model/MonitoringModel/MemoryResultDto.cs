namespace DataService.Model.MonitoringModel
{
    public class MemoryResultDto
    {
        public string Type { get; } = ResMonType.Memory.ToString();
        public double UsedPercent { get; set; }
        public double TotalMemoryMb { get; set; }
    }
}