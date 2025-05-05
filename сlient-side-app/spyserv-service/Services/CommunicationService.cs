using Serilog;
using System.Text;
using Newtonsoft.Json;
using spyserv_services.Core.Dtos;
using spyserv.Core;

namespace spyserv_services.Services
{
    public class CommunicationService
    {
        private readonly HttpClient _httpClient = new() { BaseAddress = new Uri("https://your-server.com/api/") }; //complete

        public async Task NotifyNotWorkingApp(MonitoredApp app)
        {
            try
            {
                var jsonContent = new StringContent(JsonConvert.SerializeObject(app, Formatting.Indented), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("app-data", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Error while sending monitoring data: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while trying sending monitoring data: {ex.Message}");
            }
        }

        public async Task SendMonitoringData(MonitoringData dto)
        {
            try
            {
                var jsonContent = new StringContent(JsonConvert.SerializeObject(dto, Formatting.Indented), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("MonitoringData", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Error while sending monitoring data: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while trying sending monitoring data: {ex.Message}");
            }
        }
    }
}