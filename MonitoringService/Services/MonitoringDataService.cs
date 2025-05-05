using MonitoringService.AsyncDataServices;
using MonitoringService.SyncDataServices.Grpc;

namespace MonitoringService.Services
{
    public class MonitoringDataService(
        MonitoringMessageBusPublisher messageBus,
        GrpcMonitoringCommunicationService grpcClient) : IMonitoringDataService
    {
        private readonly MonitoringMessageBusPublisher _messageBus = messageBus;
        private readonly GrpcMonitoringCommunicationService _grpcClient = grpcClient;

        public async Task UpdateDataAsync(string userEmail, string deviceName, Dto.MonitoringData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            await _messageBus.PublishMonitoringDataAsync(userEmail, deviceName, data);
        }


        public async Task<MonitoringData?> GetLatestAsync(string userEmail, string deviceName)
        {
            return await _grpcClient.GetLatestMonitoringDataAsync(userEmail, deviceName);
        }
    }
}