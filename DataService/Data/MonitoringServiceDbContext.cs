using DataService.Model.MonitoringModel;
using Microsoft.EntityFrameworkCore;

namespace DataService.Data
{
    public class MonitoringServiceDbContext(DbContextOptions<MonitoringServiceDbContext> options) : DbContext(options)
    {
        public DbSet<ClientApp> ClientApps { get; set; } = null!;
        public DbSet<Model.MonitoringModel.MonitoringData> MonitoringData { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientApp>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<ClientApp>()
                .HasOne(c => c.User) 
                .WithMany() 
                .HasForeignKey(c => c.User!.Id) 
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<Model.MonitoringModel.MonitoringData>()
                .HasKey(m => m.Id);

            base.OnModelCreating(modelBuilder);
        }
    }
}