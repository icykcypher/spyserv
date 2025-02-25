using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public interface IAuthorizationGrpcService
    {
        Task<IEnumerable<RolePermissions>> GetRolePermissionsAsync(IConfiguration configuration);
    }
}