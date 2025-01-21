using UserService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserService.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
    {
        private readonly AuthorizationOptions _authorization;

        public RolePermissionConfiguration(AuthorizationOptions authorization)
        {
            this._authorization = authorization;
        }

        public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
        {
            builder.HasKey(r => new { r.RoleId, r.PermissionId });

            builder.HasData(ParseRolePermissions());
        }

        private List<RolePermissionEntity> ParseRolePermissions()
        {
            return _authorization.RolePermissions
                .SelectMany(rp => rp.Permissions
                    .Select(p => new RolePermissionEntity
                    {
                        RoleId = (int)Enum.Parse<Role>(rp.Role),
                        PermissionId = (int)Enum.Parse<Permission>(p)
                    }))
                .ToList();
        }
    }
}