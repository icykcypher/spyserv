namespace DataService.Model.MonitoringModel
{
    public class MonitoringMessageDto
    {
        public string UserEmail { get; set; } = default!;
        public string DeviceName { get; set; } = default!;
        public MonitoringData Data { get; set; } = default!;
    }
}