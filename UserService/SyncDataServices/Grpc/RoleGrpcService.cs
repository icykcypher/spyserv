using Serilog;
using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class RoleGrpcService : IRoleGrpcService
    {
        public async Task<IEnumerable<RoleEntity>> GetRolesAsync(IConfiguration configuration)
        {
            using var channel = GrpcChannel.ForAddress(configuration["DataService"] ??
                throw new NullReferenceException($"{this.GetType()}: the adress for DataService was not specified in appsettings.json"));

            var client = new RoleGrpc.RoleGrpcClient(channel);

            try
            {
                var response = await client.GetRolesAsync(new Google.Protobuf.WellKnownTypes.Empty());

                return response.Roles.Select(x => new RoleEntity
                {
                    Id = x.Id,
                    Name = x.Name
                });
            }
            catch (Exception e)
            {
                Log.Error(e, $"{this.GetType()}: Failed to get Roles from DataService gRPC");
                Console.WriteLine($"Failed to get Roles from DataService gRPC: {e.Message}");
                throw;
            }
        }
    }
}