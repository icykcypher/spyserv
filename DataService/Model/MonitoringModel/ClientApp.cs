using DataService.Model.UsersModel;

namespace DataService.Model.MonitoringModel
{
    public class ClientApp
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;
        public string? IpAddress { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }

        public string? RefreshToken { get; set; }

        public Guid UserId { get; set; }  
        public User? User { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<MonitoredApp> MonitoredApps { get; set; } = [];
    }
}