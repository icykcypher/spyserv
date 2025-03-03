using Serilog;
using Serilog.Events;
using DataService.Model.UsersModel;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using DataService.Configurations.UsersConfigurations;

namespace DataService.Data
{
    public class UserServiceDbContext(
        DbContextOptions<UserServiceDbContext> options,
        IOptions<AuthorizationOptions> authOptions, IWebHostEnvironment environment, IConfiguration configuration) : DbContext(options)
    {
        private readonly IOptions<AuthorizationOptions> _authOptions = authOptions;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly IConfiguration _configuration = configuration;

        public DbSet<User> Users { get; set; }

        public DbSet<RoleEntity> Roles { get; set; }

        public DbSet<PermissionEntity> PermissionEntity { get; set; }

        public DbSet<RolePermissionEntity> RolePermissionEntity { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration["ConnectionStrings:postgre"], sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
            });

            if (_environment.IsProduction())
            {
                optionsBuilder.LogTo(message =>
                {
                    var logLevel = message.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                        ? LogEventLevel.Error
                        : LogEventLevel.Warning;

                    Log.Write(logLevel, "[Database] {Message}", message);
                },
                [ DbLoggerCategory.Database.Command.Name ],
                LogLevel.Warning);
            }
            else
            {
                optionsBuilder.LogTo(Console.WriteLine, [ DbLoggerCategory.Database.Command.Name ], LogLevel.Information)
                .EnableSensitiveDataLogging();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserServiceDbContext).Assembly);

            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration(_authOptions.Value));

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}