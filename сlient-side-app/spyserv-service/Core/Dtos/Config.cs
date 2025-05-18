namespace spyserv_services.Core.Dtos
{
    /// <summary>
    /// Classes representing config.json
    /// </summary>
    /// 
    public class MonitoredAppsConfig
    {
        public List<MonitoredApp>? MonitoredApps { get; set; } = [];
    }

    public class AppConfig
    {
        public ServicesSettings? AppSettings;
        public ResourceMonitoringSettings? ResMonSettings;
        public User? User;
        public DebugConfig? Debug { get; set; }
        public ReleaseConfig? Release { get; set; }
    }

    public class User 
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
    }

    public class DebugConfig
    {
        public Pathes? Pathes { get; set; }
    }

    public class ReleaseConfig
    {
        public Pathes? Pathes { get; set; }
    }

    public class Pathes
    {
        public string SpyservApi { get; set; } = string.Empty;
        public string SpyservWatcher { get; set; } = string.Empty;
    }

    public class ServicesSettings
    {
        public bool CheckApplicationsStatus { get; set; } = true;
        public bool SendMonitoringData { get; set; } = true;
        public bool SendNotifications { get; set; } = true;
        public bool SoftExiting { get; set; } = true;
        public int MonitoringInterval
        {
            get => _monitoringInterval;
            set
            {
                if (value < 0) throw new ArgumentException($"MonitoringInterval should be >0 instead of {value}");
                _monitoringInterval = value;
            }
        }
        public bool EnableLogging { get; set; } = true;

        private int _monitoringInterval = 60;
    }


    public class ResourceMonitoringSettings
    {
        public bool MonitorCpuUsage { get; set; } = true;
        public bool MonitorMemoryUsage { get; set; } = true;
        public bool MonitorDiskUsage { get; set; } = true;
        public int CpuUsageThreshold
        {
            get => _cpuUsageThreshold;
            set
            {
                if (value < 0 || value > 100) throw new ArgumentException($"CpuUsageThreshold should be >0 and <=100, instead of {value}");
                _cpuUsageThreshold = value;
            }
        }
        public int MemoryUsageThreshold
        {
            get => _memoryUsageThreshold;
            set
            {
                if (value < 0 || value > 100) throw new ArgumentException($"MemoryUsageThreshold should be >0 and <=100, instead of {value}");
                _memoryUsageThreshold = value;
            }
        }

        public int DiskUsageThreshold
        {
            get => _diskUsageThreshold;
            set
            {
                if (value < 0 || value > 100) throw new ArgumentException($"DiskUsageThreshold should be >0 and <=100, instead of {value}");
                _diskUsageThreshold = value;
            }
        }

        private int _cpuUsageThreshold = 80;
        private int _memoryUsageThreshold = 90;
        private int _diskUsageThreshold = 90;
    }
    public class MonitoredApp
    {
        public string Name { get; set; } = string.Empty;
        public string PathToBin { get; set; } = string.Empty;
        public string PathToLogs { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRunning { get; set; } = true;
        public bool AutoRestart { get; set; } = false;
        public int CheckingIntervalInSec { get; set; } = 60;

        private int _restartDelay = 0;
        public int RestartDelay
        {
            get => _restartDelay;
            set => _restartDelay = value > 0 || AutoRestart ? value : 0;
        }

        public bool NoNotify { get; set; } = false;

        public override bool Equals(object? obj) => obj is MonitoredApp app && app.Name == Name;

        public override int GetHashCode() => Name.GetHashCode();
    }
}