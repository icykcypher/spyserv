using Grpc.Net.Client;
using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public class RoleGrpcService(IConfiguration configuration, Serilog.ILogger logger) : IRoleGrpcService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly Serilog.ILogger _logger = logger;

        public async Task<IEnumerable<RoleEntity>> GetRolesAsync()
        {
            using var channel = GrpcChannel.ForAddress(_configuration["DataService"] ??
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
                _logger.Error(e, $"{this.GetType()}: Failed to get Roles from DataService gRPC");
                Console.WriteLine($"Failed to get Roles from DataService gRPC: {e.Message}");
                throw;
            }
        }
    }
}