using UserService.Model;
using UserService.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace UserService.Data
{
    public class AuthenticationDbContext(
       DbContextOptions<AuthenticationDbContext> options,
       IOptions<AuthorizationOptions> authOptions) : DbContext(options)
    {
        private readonly IOptions<AuthorizationOptions> _authOptions = authOptions;

        public DbSet<User> Users { get; set; }
        public DbSet<RoleEntity> Roles { get; set; }

        public DbSet<PermissionEntity> PermissionEntity { get; set; }

        public DbSet<RolePermissionEntity> RolePermissionEntity { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=postgres;Username=postgres;Password=admin", sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
            })
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
                .EnableSensitiveDataLogging()
                ;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthenticationDbContext).Assembly);

            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration(_authOptions.Value));
        }
    }
}