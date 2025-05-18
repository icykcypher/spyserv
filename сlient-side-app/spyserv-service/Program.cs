using Newtonsoft.Json;
using Serilog;
using spyserv_services.Core.Dtos;
using spyserv_services.Services;

namespace spyserv_services
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists($"{Path.Combine(AppContext.BaseDirectory, @"../logs/")}"))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, @"../logs/"));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, @"../logs/spyserv-services.log"),
                rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("App started. Current Directory: {Directory}", AppContext.BaseDirectory);
            var config = LoadAppSettingsConfig(StaticClaims.PathToConfig);
            var monitoringService = new MonitoringService(config);

            monitoringService.MonitorApps(config);
        }
        private static AppConfig LoadAppSettingsConfig(string configFilePath)
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