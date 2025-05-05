namespace DataService.Model.MonitoringModel
{
    public class DiskResultDto
    {
        public string Type { get; } = ResMonType.Disk.ToString();
        public string Device { get; set; } = string.Empty;
        public double ReadMbps { get; set; }
        public double WriteMbps { get; set; }
    }
}