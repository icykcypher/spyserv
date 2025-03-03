using DataService.Model.UsersModel;

namespace DataService.StorageRepositories
{
    public interface IUserStorageRepository
    {
        Task<User?> AddNewUserAsync(User user);

        Task<List<User>?> GetAllUsersAsync();

        Task<User?> GetUserByIdAsync(Guid id);

        Task<User?> DeleteUserByIdAsync(Guid id);

        Task<User?> GetUserByEmail(string email);

        Task<ICollection<RoleEntity>> GetUserPermissions(Guid id);
    }
}