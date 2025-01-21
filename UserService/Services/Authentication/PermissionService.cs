using UserService.Model;
using UserService.StorageRepositories;

namespace UserService.Services.Authentication
{
    public class PermissionService : IPermissionService
    {
        private readonly IUserStorageRepository _repository;

        public PermissionService(IUserStorageRepository repository)
        {
            this._repository = repository;
        }

        public async Task<ICollection<RoleEntity>> GetPermissionsAsync(Guid userId)
        {
            return await _repository.GetUserPermissions(userId);
        }
    }
}