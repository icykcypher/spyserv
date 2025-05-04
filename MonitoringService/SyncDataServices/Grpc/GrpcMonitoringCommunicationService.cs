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
    }
}