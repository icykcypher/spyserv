using UserService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserService.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasMany(x => x.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<UserRoleEntity>(
                    l => l.HasOne<RoleEntity>().WithMany().HasForeignKey(r => r.RoleId),
                    r => r.HasOne<User>().WithMany().HasForeignKey(l => l.UserId));
        }
    }
}