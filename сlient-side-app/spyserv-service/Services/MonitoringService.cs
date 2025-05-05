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
        private readonly Timer? _checkingTimer;
        private List<MonitoredApp> _monitoredApps;
        private readonly Dictionary<string, DateTime> _lastCheckTimes;
        private readonly CommunicationService _communicationService;
        private readonly ServicesSettings _servicesSettings;
        private readonly ResourceMonitoringSettings _resMonSettings;

        public MonitoringService(CommunicationService communicationService)
        {
            var config = LoadAppSettingsConfig(StaticClaims.PathToConfig);
            _servicesSettings = config.AppSettings ?? new ServicesSettings();
            _resMonSettings = config.ResMonSettings ?? new ResourceMonitoringSettings();

            _monitoredApps = GetMonitoredApps();
            _lastCheckTimes = _monitoredApps.ToDictionary(app => app.Name, _ => DateTime.MinValue);
            _communicationService = communicationService;

            if (_servicesSettings.CheckApplicationsStatus)
            {
                _checkingTimer = new Timer(MonitorApps, null, 0, _servicesSettings.MonitoringInterval * 1000);
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
                await Task.Run(() => _communicationService.SendMonitoringData(GetResourceUsage()));
            }
        }

        private List<MonitoredApp> GetMonitoredApps()
        {
            var config = LoadMonitoredAppsConfig(StaticClaims.PathToMonitoredAppsConf);
            return config.MonitoredApps;
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
                    await Task.Run(() => _communicationService.NotifyNotWorkingApp(app));
                }
            }
            else
            {
                app.IsRunning = true;
            }
        }

        private void RestartApplication(MonitoredApp app)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(app.Name) || !string.IsNullOrWhiteSpace(app.PathToBin))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = app.Name,
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
                    var cpuUsage = _resMonSettings.MonitorCpuUsage ? ResourceMonitorService.GetCpuUsagePercentage() : new();
                    var memoryUsage = _resMonSettings.MonitorMemoryUsage ? ResourceMonitorService.GetMemoryUsage() : new();
                    var diskUsage = _resMonSettings.MonitorDiskUsage ? ResourceMonitorService.GetDiskUsage() : new();

                    return new MonitoringData { CpuResult = cpuUsage, MemoryResult = memoryUsage, DiskResult = diskUsage };
                }
                catch (Exception ex)
                {
                    Log.Error($"Error retrieving system resource usage: {ex.Message}");
                    throw;
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
                throw new Exception();
            }
        }

        private AppConfig LoadAppSettingsConfig(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }
            else
            {
                Log.Error($"Configuration file appsettings.json does not exists or wasn't found at {configFilePath}");
                Environment.Exit(1);
                throw new Exception();
            }
        }
    }
}