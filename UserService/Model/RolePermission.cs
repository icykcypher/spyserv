namespace UserService.Model
{
    public class RolePermissions
    {
        public string Role { get; set; } = string.Empty;

        public string[] Permissions { get; set; } = [];
    }
}