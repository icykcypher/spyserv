using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class CommunicationGrpcService(IConfiguration configuration, Serilog.ILogger logger)
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly Serilog.ILogger _logger = logger;

        public async Task<IEnumerable<RolePermissions>> GetUserByEmailAsync(string email)
        {
            using var channel = GrpcChannel.ForAddress(_configuration["DataService"] ??
                throw new NullReferenceException($"{this.GetType()}: the adress for DataService was not specified in appsettings.json"));

            var client = new AuthorizationGrpc.AuthorizationGrpcClient(channel);

            try
            {
                
            }
            catch (Exception e)
            {
                _logger.Error(e, $"{this.GetType()}: Failed to get Role Permissions from DataService gRPC");
                Console.WriteLine($"Failed to get Role Permissions from DataService gRPC: {e.Message}");
                throw;
            }

            throw new NotImplementedException();
        }

    }
}