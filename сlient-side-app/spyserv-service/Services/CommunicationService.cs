using Serilog;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using spyserv_services.Core.Dtos;

namespace spyserv_services.Services
{
    public class CommunicationService
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer = new();
        private readonly string _deviceName;
        private readonly string _userEmail;
        private string? _authToken;
        private bool _isRegistered = false;

        public CommunicationService(AppConfig config)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/api/m/")
            };

            _deviceName = "Ubuntu";
            _userEmail = config.User?.Email ?? throw new InvalidOperationException("User email not configured");
        }

        private readonly object _registrationLock = new();

        public void RegisterDevice()
        {
            lock (_registrationLock)
            {
                if (_isRegistered) return;

                try
                {
                    var request = new RegisterDeviceRequest
                    {
                        UserEmail = _userEmail,
                        DeviceName = _deviceName
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = _httpClient.PostAsync("register", content).Result;

                    var responseText = response.Content.ReadAsStringAsync().Result;
                    Log.Warning(responseText);

                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"Device registration failed: {response.StatusCode}");
                        Environment.Exit(1);
                    }

                    var result = JsonConvert.DeserializeObject<RegisterDeviceResponse>(responseText);
                    _authToken = result?.Token;

                    if (_authToken != null)
                    {
                        var baseUri = new Uri("http://localhost/");
                        _cookieContainer.Add(baseUri, new Cookie("homka-lox", _authToken)
                        {
                            HttpOnly = true,
                            Secure = false,
                            Path = "/"
                        });

                        _isRegistered = true;
                        Log.Information("Device registered successfully and cookie added");
                    }
                    else
                    {
                        Log.Error("No auth token received.");
                        Environment.Exit(1);
                    }

                    LogCookies();
                }
                catch (Exception ex)
                {
                    Log.Error($"Error during device registration: {ex}");
                    Environment.Exit(1);
                }
            }
        }

        public void SendMonitoringData(MonitoringDataRequest data)
        {
            try
            {
                EnsureAuthToken();

                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                int attempts = 0;
                HttpResponseMessage? response = null;

                do
                {
                    response = _httpClient.PostAsync($"data/{_deviceName.ToLower()}", content).Result;
                    if (response.IsSuccessStatusCode)
                        break;

                    attempts++;
                }
                while (attempts < 3);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    Log.Error($"Failed to send monitoring data after {attempts} attempts. Status: {response?.StatusCode}");
                }
                else
                {
                    Log.Information("Successfully sent monitoring data");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending monitoring data: {ex}");
            }
        }

        public void SendAppStatuses(List<AppStatusDto> statuses)
        {
            try
            {
                EnsureAuthToken();

                var request = new AppStatusesRequest
                {
                    UserEmail = _userEmail,
                    DeviceName = _deviceName,
                    Statuses = statuses
                };

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var response = _httpClient.PostAsync($"statuses/{_deviceName.ToLower()}", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Error sending app statuses: {response.StatusCode}");
                }
                else
                {
                    Log.Information("Successfully sent monitored app statuses");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending app statuses: {ex}");
            }
        }

        private void EnsureAuthToken()
        {
            if (string.IsNullOrWhiteSpace(_authToken))
            {
                Log.Warning("No auth token present. Registering device again.");
                RegisterDevice();
            }
        }

        private void LogCookies()
        {
            var baseUri = new Uri("http://localhost/");
            var cookies = _cookieContainer.GetCookies(baseUri);

            Log.Warning($"Cookie count: {cookies.Count}");
            foreach (Cookie cookie in cookies)
            {
                Log.Information($"Cookie received: {cookie.Name} = {cookie.Value}");
            }
        }
    }
}