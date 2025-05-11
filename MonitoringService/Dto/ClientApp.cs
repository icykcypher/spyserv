namespace MonitoringService.Dto
{
    public class ClientApp
    {
        public Guid Id { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;
        public string? IpAddress { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }
}