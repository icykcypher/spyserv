using Grpc.Core;
using DataService.Data;
using Microsoft.EntityFrameworkCore;

namespace DataService.SyncDataServices.Grpc.MonitoringService
{
    public class GrpcMonitoringService(MonitoringServiceDbContext dbContext, UserServiceDbContext userDbContext) : MonitoringGrpcService.MonitoringGrpcServiceBase
    {
        private readonly MonitoringServiceDbContext _monitoringDbContext = dbContext;
        private readonly UserServiceDbContext _userDbContext = userDbContext;

        public override async Task<MonitoringData> GetLatest(GetLatestRequest request, ServerCallContext context)
        {
            var clientAppId = Guid.Parse(request.ClientAppId);

            var latestMonitoringData = await _monitoringDbContext.MonitoringData
                .Where(m => m.ClientAppId == clientAppId)
                .OrderByDescending(m => m.Timestamp) 
                .FirstOrDefaultAsync();

            if (latestMonitoringData == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "No monitoring data found for the given ClientAppId"));
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
            var exists = await _userDbContext.Users
                .AnyAsync(c => c.Email == request.Email); 

            return new UserExistsResponse { Exists = exists };
        }
    }
}