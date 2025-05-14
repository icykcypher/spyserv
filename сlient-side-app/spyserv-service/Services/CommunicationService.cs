using Serilog;
using System.Text;
using Newtonsoft.Json;
using spyserv_services.Core.Dtos;
using spyserv.Core;

namespace spyserv_services.Services
{
    public class CommunicationService
    {
        private readonly HttpClient _httpClient;
        private string? _authToken;
        private readonly string _deviceName;
        private readonly string _userEmail;
        private bool _isRegistered = false;

        public CommunicationService(AppConfig config)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost/api/m/") };
            _deviceName = Environment.MachineName;
            _userEmail = config.User?.Email ?? throw new InvalidOperationException("User email not configured");
        }

        public async Task RegisterDevice()
        {
            if (_isRegistered) return;

            try
            {
                var request = new RegisterDeviceRequest
                {
                    UserEmail = _userEmail,
                    DeviceName = _deviceName
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("register", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<RegisterDeviceResponse>(responseContent);
                    _authToken = result?.AuthToken;
                    _isRegistered = true;
                    Log.Information("Device registered successfully");
                }
                else
                {
                    Log.Error($"Device registration failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during device registration: {ex.Message}");
            }
        }

        public async Task SendMonitoringData(MonitoringDataRequest data)
        {
            if (!_isRegistered) await RegisterDevice();

            try
            {
                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(data),
                    Encoding.UTF8,
                    "application/json");

                if (!string.IsNullOrEmpty(_authToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                }

                var response = await _httpClient.PostAsync($"data/{_deviceName}", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Error sending monitoring data: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending monitoring data: {ex.Message}");
            }
        }

        public async Task SendAppStatuses(List<AppStatusDto> statuses)
        {
            if (!_isRegistered) await RegisterDevice();

            try
            {
                var request = new AppStatusesRequest
                {
                    UserEmail = _userEmail,
                    DeviceName = _deviceName,
                    Statuses = statuses
                };

                var jsonContent = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");

                if (!string.IsNullOrEmpty(_authToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                }

                var response = await _httpClient.PostAsync($"statuses/{_deviceName}", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Error sending app statuses: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending app statuses: {ex.Message}");
            }
        }

        public async Task NotifyNotWorkingApp(MonitoredApp app)
        {
            // Можно модифицировать или оставить как есть
            // В зависимости от требований к нотификациям
        }
    }
}