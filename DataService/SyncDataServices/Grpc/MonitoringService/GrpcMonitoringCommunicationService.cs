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
    }
}