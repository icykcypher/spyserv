using UserService.Model;
using Microsoft.AspNetCore.Authorization;

namespace UserService.Services.Authentication
{
    public class PermissionRequirement(ICollection<RoleEntity> permissions) : IAuthorizationRequirement
    {
        public ICollection<RoleEntity> Permissions { get; set; } = permissions;
    }
}