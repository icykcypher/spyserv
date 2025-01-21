using UserService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserService.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<PermissionEntity>
    {
        public void Configure(EntityTypeBuilder<PermissionEntity> builder)
        {
            builder.HasKey(x => x.Id);
            var permissions = Enum
                .GetValues<Permission>()
                .Select(x => new PermissionEntity
                {
                    Id = (int)x,
                    Name = x.ToString()
                });

            builder.HasData(permissions);
        }
    }
}