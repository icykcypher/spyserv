using DataService.Model.UsersModel;

namespace DataService.Model.MonitoringModel
{
    public class ClientApp
    {
        public Guid Id { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;
        public string? IpAddress { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string? RefreshToken { get; set; }

        public User? User { get; set; }
    }
}