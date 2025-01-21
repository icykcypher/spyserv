using UserService.Model;

namespace UserService.Services.Authentication
{
    public interface IPermissionService
    {
        Task<ICollection<RoleEntity>> GetPermissionsAsync(Guid userId);
    }
}