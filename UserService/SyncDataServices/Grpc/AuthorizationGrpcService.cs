using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class AuthorizationGrpcService(IConfiguration configuration, Serilog.ILogger logger) : IAuthorizationGrpcService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly Serilog.ILogger _logger = logger;

        public async Task<IEnumerable<RolePermissionEntity>> GetRolePermissionsAsync()
        {
            using var channel = GrpcChannel.ForAddress(_configuration["DataService"] ??
                throw new NullReferenceException($"{this.GetType()}: the adress for DataService was not specified in appsettings.json"));

            var client = new AuthorizationGrpc.AuthorizationGrpcClient(channel);

            try
            {
                var response = await client.GetRolePermissionsAsync(new Google.Protobuf.WellKnownTypes.Empty());

                return response.RolePermissions.Select(x => new RolePermissionEntity
                {
                    RoleId = x.RoleId,
                    PermissionId = x.PermissionId
                });
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