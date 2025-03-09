using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class PermissionGrpcService(IConfiguration configuration, Serilog.ILogger logger) : IPermissionGrpcService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly Serilog.ILogger _logger = logger;

        public async Task<IEnumerable<PermissionEntity>> GetPermissionsAsync()
        {
            using var channel = GrpcChannel.ForAddress(_configuration["DataService"] ??
                throw new NullReferenceException($"{this.GetType()}: the adress for DataService was not specified in appsettings.json"));

            var client = new PermissionGrpc.PermissionGrpcClient(channel);
            try
            {
                var response = await client.GetPermissionsAsync(new Google.Protobuf.WellKnownTypes.Empty());
                return response.Permissions.Select(x => new PermissionEntity { Id = x.Id, Name = x.Name });
            }
            catch (Exception e)
            {
                _logger.Error(e, $"{this.GetType()}: Failed to get Permissions from DataService gRPC");
                Console.WriteLine($"Failed to get Permissions from DataService gRPC: {e.Message}");
                throw;
            }
            throw new NotImplementedException();
        }
    }
}