using UserService.Model;

namespace UserService.Configurations
{
    public class AuthorizationOptions
    {
        public RolePermissions[] RolePermissions { get; set; } = [];
    }
}