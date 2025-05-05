using DataService.Data;
using Microsoft.EntityFrameworkCore;
using DataService.Model.MonitoringModel;

namespace DataService.StorageRepositories
{
    public class MonitoringStorageRepository(MonitoringUserServiceDbContext context) : IMonitoringStorageRepository
    {
        private readonly MonitoringUserServiceDbContext _context = context;

        public async Task<ClientApp> AddNewClientAppAsync(ClientApp clientApp)
        {
            clientApp.User = await _context.Users.Where(u => u.Email == clientApp.UserEmail).FirstAsync();

            _context.ClientApps.Add(clientApp);
            await _context.SaveChangesAsync();
            return clientApp;
        }
    }
}