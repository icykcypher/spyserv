using Microsoft.AspNetCore.Authorization;
using UserService.Model;

namespace UserService.Services.Authentication
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(ICollection<RoleEntity> permissions)
        {
            this.Permissions = permissions;
        }

        public ICollection<RoleEntity> Permissions { get; set; } = [];
    }
}