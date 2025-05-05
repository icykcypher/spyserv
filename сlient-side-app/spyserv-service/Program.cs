using Serilog;
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

            var communicationService = new CommunicationService();
            var monitoringService = new MonitoringService(communicationService);

            monitoringService.MonitorApps(communicationService);
        }
    }
}