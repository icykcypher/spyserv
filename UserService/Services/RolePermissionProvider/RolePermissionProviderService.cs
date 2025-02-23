using UserService.Model;
using UserService.SyncDataServices.Grpc;

namespace UserService.Services.RolePermissionProvider
{
    public class RolePermissionProviderService(AuthorizationGrpcService grpcService, IConfiguration configuration)
    {
        private readonly AuthorizationGrpcService _grpcService = grpcService;
        private readonly IConfiguration _configuration = configuration;

        public async Task<List<RoleEntity>> GetRolePermissionsAsync()
        {
            var rolePermissionsGrpc = await _grpcService.GetRolePermissionsAsync(_configuration);

            return rolePermissionsGrpc.Select(rp => new RoleEntity
            {
                Name = rp.Role,
                Permissions = rp.Permissions.Select(p => new PermissionEntity
                {
                    Name = p
                }).ToArray()
            }).ToList();
        }
    }
}