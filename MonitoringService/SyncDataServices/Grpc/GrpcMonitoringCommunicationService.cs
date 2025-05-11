using MonitoringService.Dto;

namespace MonitoringService.SyncDataServices.Grpc
{
    public class GrpcMonitoringCommunicationService(MonitoringGrpcService.MonitoringGrpcServiceClient grpcClient)
    {
        private readonly MonitoringGrpcService.MonitoringGrpcServiceClient _grpcClient = grpcClient;

        public async Task<MonitoringData?> GetLatestMonitoringDataAsync(string userEmail, string deviceName)
        {
            var request = new GetLatestRequest
            {
                UserEmail = userEmail,
                DeviceName = deviceName
            };

            var response = await _grpcClient.GetLatestAsync(request);

            ArgumentNullException.ThrowIfNull(response);

            return new MonitoringData
            {
                CpuResult = response.CpuResult,
                MemoryResult = response.MemoryResult,
                DiskResult = response.DiskResult
            };
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var request = new UserExistsRequest
            {
                Email = email
            };

            var response = await _grpcClient.UserExistsAsync(request);

            return response.Exists;
        }

        public async Task<List<ClientApp>> GetUserAppsAsync(string userId)
        {
            var request = new GetUserAppsRequest
            {
                UserId = userId
            };

            var response = await _grpcClient.GetUserAppsAsync(request);

            return response.Apps.Select(app => new ClientApp
            {
                Id = Guid.Parse(app.AppId),
                Description = app.Description,
                DeviceName = app.AppName,
                IsActive = app.Status == "online",
            }).ToList();
        }

        public async Task<List<MonitoringAppStatusDto>> GetAppStatusesAsync(string userEmail, string deviceName)
        {
            var request = new GetAppStatusesRequest
            {
                UserEmail = userEmail,
                DeviceName = deviceName
            };

            var response = await _grpcClient.GetAppStatusesAsync(request);

            return response.Statuses.Select(status => new MonitoringAppStatusDto
            {
                AppName = status.AppName,
                CpuUsagePercent = status.CpuUsagePercent,
                MemoryUsagePercent = status.MemoryUsagePercent,
                LastStarted = DateTime.Parse(status.LastStarted),
                IsRunning = status.IsRunning,
            }).ToList();
        }
    }
}