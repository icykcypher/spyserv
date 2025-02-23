using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public interface IPermissionGrpcService
    {
        Task<IEnumerable<Permission>> GetPermissionsAsync(IConfiguration configuration);
    }
}