using Serilog;
using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class PermissionGrpcService : IPermissionGrpcService
    {
        public async Task<IEnumerable<Permission>> GetPermissionsAsync(IConfiguration configuration)
        {
            using var channel = GrpcChannel.ForAddress(configuration["DataService"] ??
                throw new NullReferenceException($"{this.GetType()}: the adress for DataService was not specified in appsettings.json"));

            var client = new PermissionGrpc.PermissionGrpcClient(channel);
            try
            {
                var response = await client.GetPermissionsAsync(new Google.Protobuf.WellKnownTypes.Empty());
                return response.Permissions.Select(x => (Permission)x.Id);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{this.GetType()}: Failed to get Permissions from DataService gRPC");
                Console.WriteLine($"Failed to get Permissions from DataService gRPC: {e.Message}");
                throw;
            }
            throw new NotImplementedException();
        }
    }
}