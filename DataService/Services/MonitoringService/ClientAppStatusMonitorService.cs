using DataService.Data;
using DataService.Model.MonitoringModel;
using Microsoft.EntityFrameworkCore;

namespace DataService.Services
{
    public class ClientAppStatusMonitorService(ILogger<ClientAppStatusMonitorService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
    {
        private readonly ILogger<ClientAppStatusMonitorService> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringUserServiceDbContext>();

                    var now = DateTime.UtcNow;
                    var oneMinuteAgo = now.AddMinutes(-1);

                    // Получаем клиентские приложения, которые не были активны более 1 минуты
                    var inactiveClientApps = await dbContext.ClientApps
                        .Include(c => c.MonitoredApps)
                        .Where(c => c.IsActive)
                        .Where(c => !dbContext.MonitoringData
                            .Where(m => m.ClientApp.Id == c.Id)
                            .OrderByDescending(m => m.Timestamp)
                            .Select(m => m.Timestamp)
                            .Take(1)
                            .Any(t => t > oneMinuteAgo))
                        .ToListAsync(stoppingToken);

                    foreach (var clientApp in inactiveClientApps)
                    {
                        // Останавливаем все приложения клиента
                        clientApp.IsActive = false;
                        dbContext.ClientApps.Update(clientApp);

                        foreach (var app in clientApp.MonitoredApps)
                        {
                            // Обновляем статус работы приложения
                            app.IsRunning = false;
                            dbContext.MonitoredApps.Update(app);

                            // Добавляем запись в историю статусов приложения
                            var status = new MonitoredAppStatus
                            {
                                MonitoredAppId = app.Id,
                                IsRunning = false,
                                CpuUsagePercent = 0,  // Можно заменить на реальные данные, если есть
                                MemoryUsagePercent = 0,  // То же самое
                                Timestamp = DateTime.UtcNow,
                                LastStarted = DateTime.UtcNow
                            };

                            dbContext.MonitoredAppStatuses.Add(status);
                        }

                        Console.WriteLine($"⚠️ ClientApp {clientApp.DeviceName} marked as INACTIVE. Also marked {clientApp.MonitoredApps.Count} apps as INACTIVE.");
                    }

                    // Сохраняем изменения в базе данных
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in ClientAppStatusMonitorService");
                    Console.WriteLine(ex);
                }

                // Ожидаем 30 секунд перед следующей проверкой
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}