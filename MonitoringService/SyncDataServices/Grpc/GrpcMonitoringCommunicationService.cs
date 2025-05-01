namespace MonitoringService.SyncDataServices.Grpc
{
    public class GrpcMonitoringCommunicationService(MonitoringGrpcService.MonitoringGrpcServiceClient grpcClient)
    {
        private readonly MonitoringGrpcService.MonitoringGrpcServiceClient _grpcClient = grpcClient;

        public async Task<MonitoringData?> GetLatestMonitoringDataAsync(string clientAppId)
        {
            var request = new GetLatestRequest
            {
                ClientAppId = clientAppId
            };

            var response = await _grpcClient.GetLatestAsync(request);

            if (response == null)
                return null;

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
    }
}