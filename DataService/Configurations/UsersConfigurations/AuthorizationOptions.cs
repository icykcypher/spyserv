using DataService.Model.UsersModel;

namespace DataService.Configurations.UsersConfigurations
{
    public class AuthorizationOptions
    {
        public RolePermissions[] RolePermissions { get; set; } = [];
    }
}