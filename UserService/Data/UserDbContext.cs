using UserService.Model;
using UserService.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace UserService.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options,
                         IOptions<AuthorizationOptions> authOptions) : DbContext(options)
    {
        private readonly IOptions<AuthorizationOptions> _authOptions = authOptions;

        public DbSet<RoleEntity> Roles { get; set; }
        public DbSet<PermissionEntity> Permissions { get; set; }
        public DbSet<RolePermissionEntity> RolePermissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("InMem")
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Warning)
                .EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration(_authOptions.Value));
        }
    }
}