using Serilog;
using Serilog.Events;
using DataService.Model.UsersModel;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using DataService.Model.MonitoringModel;
using DataService.Configurations.UsersConfigurations;

namespace DataService.Data
{
    public class MonitoringUserServiceDbContext(
        DbContextOptions<MonitoringUserServiceDbContext> options,
        IOptions<AuthorizationOptions> authOptions,
        IWebHostEnvironment environment,
        IConfiguration configuration) : DbContext(options)
    {
        private readonly IOptions<AuthorizationOptions> _authOptions = authOptions;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly IConfiguration _configuration = configuration;

        // Users DB
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RoleEntity> Roles { get; set; } = null!;
        public DbSet<PermissionEntity> PermissionEntity { get; set; } = null!;
        public DbSet<RolePermissionEntity> RolePermissionEntity { get; set; } = null!;

        // Monitoring DB
        public DbSet<ClientApp> ClientApps { get; set; } = null!;
        public DbSet<Model.MonitoringModel.MonitoringData> MonitoringData { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration["ConnectionStrings:postgres"], sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
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
                [DbLoggerCategory.Database.Command.Name],
                LogLevel.Warning);
            }
            else
            {
                optionsBuilder.LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                              .EnableSensitiveDataLogging();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Monitoring configuration ---
            modelBuilder.Entity<ClientApp>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<ClientApp>()
                .Property(c => c.UserEmail)
                .IsRequired()
                .HasMaxLength(40);

            modelBuilder.Entity<ClientApp>()
                .HasIndex(c => c.DeviceName);

            modelBuilder.Entity<ClientApp>()
                 .HasOne(c => c.User)
                 .WithMany(u => u.ClientApps)
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Model.MonitoringModel.MonitoringData>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.OwnsOne(m => m.CpuResult);
                entity.OwnsOne(m => m.MemoryResult);
                entity.OwnsOne(m => m.DiskResult);
            });

            // --- User configuration ---
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MonitoringUserServiceDbContext).Assembly);
            modelBuilder.ApplyConfiguration(new RolePermissionConfiguration(_authOptions.Value));

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
