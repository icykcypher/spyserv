namespace MonitoringService.Dto
{
    public class AppStatusRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public List<AppStatusDto> Statuses { get; set; } = new List<AppStatusDto>();
    }
}