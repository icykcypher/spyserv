using Grpc.Core;
using DataService.Data;
using Microsoft.EntityFrameworkCore;

namespace DataService.SyncDataServices.Grpc.MonitoringService
{
    public class GrpcMonitoringService(MonitoringUserServiceDbContext dbContext) : MonitoringGrpcService.MonitoringGrpcServiceBase
    {
        private readonly MonitoringUserServiceDbContext _monitoringDbContext = dbContext;

        public override async Task<MonitoringData> GetLatest(GetLatestRequest request, ServerCallContext context)
        {
            var latestMonitoringData = await _monitoringDbContext.MonitoringData
                .Where(m =>
                    m.ClientApp.DeviceName.ToLower() == request.DeviceName.ToLower() &&
                    m.ClientApp.UserEmail.ToLower() == request.UserEmail.ToLower())
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            Console.WriteLine(request.DeviceName);
            Console.WriteLine(request.UserEmail);

            if (latestMonitoringData == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "No monitoring data found for the given device"));
            }

            return new MonitoringData
            {
                CpuResult = new CpuResult { UsagePercent = latestMonitoringData.CpuResult?.UsagePercent ?? 0 },
                MemoryResult = new MemoryResult
                {
                    UsedPercent = latestMonitoringData.MemoryResult?.UsedPercent ?? 0,
                    TotalMemoryMb = latestMonitoringData.MemoryResult?.TotalMemoryMb ?? 0
                },
                DiskResult = new DiskResult
                {
                    Device = latestMonitoringData.DiskResult?.Device ?? string.Empty,
                    ReadMbps = latestMonitoringData.DiskResult?.ReadMbps ?? 0,
                    WriteMbps = latestMonitoringData.DiskResult?.WriteMbps ?? 0
                }
            };
        }

        public override async Task<UserExistsResponse> UserExists(UserExistsRequest request, ServerCallContext context)
        {
            var exists = await _monitoringDbContext.Users
                .AnyAsync(c => c.Email == request.Email);

            return new UserExistsResponse { Exists = exists };
        }

        public override async Task<GetUserAppsResponse> GetUserApps(GetUserAppsRequest request, ServerCallContext context)
        {
            var apps = await _monitoringDbContext.ClientApps
            .Where(app => app.UserId.ToString() == request.UserId)
            .ToListAsync();

            var response = new GetUserAppsResponse();
            foreach (var app in apps)
            {
                response.Apps.Add(new UserApp
                {
                    AppId = app.Id.ToString(),
                    AppName = app.DeviceName,
                    Description = app.Description,
                    Status = app.IsActive ? "online" : "offline",
                    Link = $"http://localhost/apps/{app.Id}"
                });
            }

            return response;
        }

        public override async Task<GetAppStatusesResponse> GetAppStatuses(GetAppStatusesRequest request, ServerCallContext context)
        {
            var statuses = await _monitoringDbContext.MonitoredAppStatuses
                .Include(s => s.MonitoredApp)
                .ThenInclude(a => a.ClientApp)
                .Where(s =>
                    s.MonitoredApp.ClientApp!.UserEmail.ToLower().Equals(request.UserEmail.ToLower()) &&
                    s.MonitoredApp.ClientApp.DeviceName.ToLower().Equals(request.DeviceName.ToLower()))
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();

            var response = new GetAppStatusesResponse();

            foreach (var status in statuses)
            {
                response.Statuses.Add(new MonitoredAppStatusDto
                {
                    AppName = status.MonitoredApp.Name,
                    CpuUsagePercent = status.CpuUsagePercent,
                    MemoryUsagePercent = status.MemoryUsagePercent,
                    LastStarted = status.LastStarted.ToString("o"),
                    Timestamp = status.Timestamp.ToString("o"),
                    IsRunning = status.IsRunning
                });
            }

            return response;
        }
    }
}