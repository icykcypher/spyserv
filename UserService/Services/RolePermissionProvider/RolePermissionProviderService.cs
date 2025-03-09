using UserService.Model;
using UserService.SyncDataServices.Grpc;

namespace UserService.Services.RolePermissionProvider
{
    public class RolePermissionProviderService(RoleGrpcService grpcService)
    {
        private readonly RoleGrpcService _grpcService = grpcService;

        public async Task<IEnumerable<RoleEntity>> GetRolesAsync() => await _grpcService.GetRolesAsync();

    }
}