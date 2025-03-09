using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public interface IRoleGrpcService
    {
        Task<IEnumerable<RoleEntity>> GetRolesAsync();
    }
}