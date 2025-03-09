using DataService.Model.UsersModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataService.Configurations.UsersConfigurations
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