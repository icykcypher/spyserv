using UserService.Model;

namespace UserService.SyncDataServices.Grpc
{
    public interface IGrpcUserCommunicationService
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<List<RoleEntity>> SeedAndGetRolesAsync();
    }
}