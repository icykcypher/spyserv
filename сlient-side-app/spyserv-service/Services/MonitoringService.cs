using spyserv;
using Serilog;
using spyserv.Core;
using Newtonsoft.Json;
using System.Diagnostics;
using spyserv_services.Core.Dtos;

namespace spyserv_services.Services
{
    public class MonitoringService
    {
        private readonly Timer? _monitoringTimer;
        private readonly Timer? _statusesTimer;
        private List<MonitoredApp> _monitoredApps;
        private readonly CommunicationService _communicationService;
        private readonly ServicesSettings _servicesSettings;
        private readonly ResourceMonitoringSettings _resMonSettings;
        private readonly Dictionary<string, DateTime> _lastCheckTimes;

        public MonitoringService(AppConfig config)
        {
            _servicesSettings = config.AppSettings ?? new ServicesSettings();
            _resMonSettings = config.ResMonSettings ?? new ResourceMonitoringSettings();
            _communicationService = new CommunicationService(config);
            _monitoredApps = GetMonitoredApps();
            _lastCheckTimes = _monitoredApps.ToDictionary(app => app.Name, _ => DateTime.MinValue);

            _monitoringTimer = new Timer(SendSystemMetrics, null, 0, 5000);
            _statusesTimer = new Timer(SendAppsStatuses, null, 0, 3000);

            if (_servicesSettings.CheckApplicationsStatus)
            {
                var monitorTimer = new Timer(MonitorApps, null, 0, _servicesSettings.MonitoringInterval * 1000);
            }
        }

        public async void MonitorApps(object? state)
        {
            Log.Information("Started monitoring");

            _monitoredApps = GetMonitoredApps();
            foreach (var app in _monitoredApps)
            {
                if ((DateTime.Now - _lastCheckTimes[app.Name]).TotalSeconds >= app.CheckingIntervalInSec)
                {
                    await CheckApplicationStatus(app);
                    _lastCheckTimes[app.Name] = DateTime.Now;
                }
            }

            if (_servicesSettings.SendMonitoringData)
            {
                await _communicationService.SendMonitoringData(ConvertToRequest(GetResourceUsage()));
            }
        }

        private MonitoringDataRequest ConvertToRequest(MonitoringData data)
        {
            return new MonitoringDataRequest
            {
                CpuResult = data.CpuResult,
                MemoryResult = data.MemoryResult,
                DiskResult = data.DiskResult
            };
        }

        private async void SendSystemMetrics(object? state)
        {
            try
            {
                if (_resMonSettings.MonitorCpuUsage || _resMonSettings.MonitorMemoryUsage || _resMonSettings.MonitorDiskUsage)
                {
                    var data = new MonitoringDataRequest
                    {
                        CpuResult = _resMonSettings.MonitorCpuUsage ? ResourceMonitorService.GetCpuUsagePercentage() : null,
                        MemoryResult = _resMonSettings.MonitorMemoryUsage ? ResourceMonitorService.GetMemoryUsage() : null,
                        DiskResult = _resMonSettings.MonitorDiskUsage ? ResourceMonitorService.GetDiskUsage() : null
                    };

                    await _communicationService.SendMonitoringData(data);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in SendSystemMetrics: {ex.Message}");
            }
        }

        private List<MonitoredApp> GetMonitoredApps()
        {
            var config = LoadMonitoredAppsConfig(StaticClaims.PathToMonitoredAppsConf);
            return config.MonitoredApps ?? new List<MonitoredApp>();
        }

        private async Task CheckApplicationStatus(MonitoredApp app)
        {
            Log.Information($"Checking {app.Name}");

            if (!IsAppRunning(app.Name))
            {
                app.IsRunning = false;
                Log.Information($"Application {app.Name} is not running");
                if (app.AutoRestart)
                {
                    await Task.Run(() => RestartApplication(app));
                }
                if (_servicesSettings.SendNotifications && !app.NoNotify)
                {
                    await _communicationService.NotifyNotWorkingApp(app);
                }
            }
            else
            {
                app.IsRunning = true;
            }
        }

        private async void SendAppsStatuses(object? state)
        {
            try
            {
                _monitoredApps = GetMonitoredApps();
                var statuses = new List<AppStatusDto>();

                foreach (var app in _monitoredApps)
                {
                    var status = new AppStatusDto
                    {
                        AppName = app.Name,
                        IsRunning = IsAppRunning(app.Name),
                        LastChecked = DateTime.UtcNow,
                        CpuUsagePercent = 0, // TODO: Implement actual CPU usage measurement
                        MemoryUsagePercent = 0 // TODO: Implement actual memory usage measurement
                    };

                    statuses.Add(status);
                }

                await _communicationService.SendAppStatuses(statuses);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in SendAppsStatuses: {ex.Message}");
            }
        }

        private void RestartApplication(MonitoredApp app)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(app.PathToBin))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = app.PathToBin,
                        UseShellExecute = false
                    });
                    Log.Information($"Application '{app.Name}' restarted successfully.");
                    app.IsRunning = true;
                }
                else
                {
                    Log.Error($"Cannot restart application '{app.Name}' - no path specified.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to restart application '{app.Name}': {ex.Message}");
            }
        }

        private bool IsAppRunning(string appName) => Process.GetProcessesByName(appName).Any();

        private MonitoringData GetResourceUsage()
        {
            if (_resMonSettings.MonitorCpuUsage || _resMonSettings.MonitorMemoryUsage || _resMonSettings.MonitorDiskUsage)
            {
                try
                {
                    var cpuUsage = _resMonSettings.MonitorCpuUsage ? ResourceMonitorService.GetCpuUsagePercentage() : new CpuResultDto();
                    var memoryUsage = _resMonSettings.MonitorMemoryUsage ? ResourceMonitorService.GetMemoryUsage() : new MemoryResultDto();
                    var diskUsage = _resMonSettings.MonitorDiskUsage ? ResourceMonitorService.GetDiskUsage() : new DiskResultDto();

                    return new MonitoringData
                    {
                        CpuResult = cpuUsage,
                        MemoryResult = memoryUsage,
                        DiskResult = diskUsage
                    };
                }
                catch (Exception ex)
                {
                    Log.Error($"Error retrieving system resource usage: {ex.Message}");
                    return new MonitoringData();
                }
            }

            Log.Warning("Resource monitoring is disabled in the configuration.");
            return new MonitoringData();
        }

        private MonitoredAppsConfig LoadMonitoredAppsConfig(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<MonitoredAppsConfig>(json) ?? new MonitoredAppsConfig();
            }
            else
            {
                Log.Error($"Configuration file for monitoring apps does not exists or wasn't found at {configFilePath}");
                Environment.Exit(1);
                throw new Exception("Config file not found");
            }
        }

        private AppConfig LoadAppSettingsConfig(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? throw new Exception("Invalid config");
            }
            else
            {
                Log.Error($"Configuration file appsettings.json does not exists or wasn't found at {configFilePath}");
                Environment.Exit(1);
                throw new Exception("Config file not found");
            }
        }
    }
}