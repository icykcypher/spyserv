using Serilog;
using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class AuthorizationGrpcService : IAuthorizationGrpcService
    {
        public async Task<IEnumerable<RolePermissions>> GetRolePermissionsAsync(IConfiguration configuration)
        {
            using var channel = GrpcChannel.ForAddress(configuration["DataService"] ??
                throw new NullReferenceException($"{this.GetType()}: the adress for DataService was not specified in appsettings.json"));

            var client = new AuthorizationGrpc.AuthorizationGrpcClient(channel);

            try
            {
                var response = await client.GetRolePermissionsAsync(new Google.Protobuf.WellKnownTypes.Empty());

                return response.RolePermissions.Select(x => new RolePermissions
                {
                    Role = x.Role,
                    Permissions = x.Permissions.ToArray()
                });
            }
            catch (Exception e)
            {
                Log.Error(e, $"{this.GetType()}: Failed to get Role Permissions from DataService gRPC");
                Console.WriteLine($"Failed to get Role Permissions from DataService gRPC: {e.Message}");
                throw;
            }

            throw new NotImplementedException();
        }
    }
}