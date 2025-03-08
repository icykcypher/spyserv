using UserService.Model;
using UserService.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using UserService.SyncDataServices.Grpc;

namespace UserService.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options,
                         IRoleGrpcService roleGrpcService,
                         IPermissionGrpcService permissionGrpcService,
                         IAuthorizationGrpcService authorizationGrpcService,
                         ILogger<UserDbContext> logger,
                         IOptions<AuthorizationOptions> authOptions) : DbContext(options)
    {
        private readonly IRoleGrpcService _roleGrpcService = roleGrpcService;
        private readonly IPermissionGrpcService _permissionGrpcService = permissionGrpcService;
        private readonly IAuthorizationGrpcService _authorizationGrpcService = authorizationGrpcService;
        private readonly ILogger<UserDbContext> _logger = logger;
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

        public async Task SeedDataAsync()
        {
            if (await Roles.AnyAsync() || await Permissions.AnyAsync() || await RolePermissions.AnyAsync())
            {
                return;
            }

            try
            {
                var roles = await _roleGrpcService.GetRolesAsync();
                if (roles != null && roles.Any())
                {
                    await Roles.AddRangeAsync(roles);
                    await SaveChangesAsync();
                    _logger.LogInformation("Roles seeded successfully.");
                }

                var permissions = await _permissionGrpcService.GetPermissionsAsync();
                if (permissions != null && permissions.Any())
                {
                    await Permissions.AddRangeAsync(permissions);
                    await SaveChangesAsync();
                    _logger.LogInformation("Permissions seeded successfully.");
                }

                var rolePermissions = await _authorizationGrpcService.GetRolePermissionsAsync();
                if (rolePermissions != null && rolePermissions.Any())
                {
                    await RolePermissions.AddRangeAsync(rolePermissions);
                    await SaveChangesAsync();
                    _logger.LogInformation("Role-Permissions seeded successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding data.");
            }
        }
    }
}