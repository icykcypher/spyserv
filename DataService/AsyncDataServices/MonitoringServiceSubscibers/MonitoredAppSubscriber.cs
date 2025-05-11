using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using DataService.Data;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;
using DataService.Model.MonitoringModel;

namespace DataService.AsyncDataServices.MonitoringServiceSubscibers
{
    public class MonitoredAppSubscriber(IServiceScopeFactory scopeFactory, IConfiguration configuration) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private IChannel _channel = null!;
        private IConnection _connection = null!;
        private IConfiguration _configuration = configuration;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("--> Starting MonitoredAppSubscriber...");
            await InitializeRabbitMQAsync();
            _ = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
            Console.WriteLine("--> MonitoredAppSubscriber started");
        }

        private async Task InitializeRabbitMQAsync()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQHost"]!,
                Port = int.Parse(_configuration["RabbitMQPort"]!),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(exchange: "monitoring.apps.status", type: ExchangeType.Direct);
            await _channel.QueueDeclareAsync(queue: "monitoring.apps.status.queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
            await _channel.QueueBindAsync(queue: "monitoring.apps.status.queue", exchange: "monitoring.apps.status", routingKey: "apps.status.update");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    var message = JsonConvert.DeserializeObject<MonitoringAppStatusDto>(json);
                    Console.WriteLine(json);
                    if (message is null)
                    {
                        Console.WriteLine("--> Failed to deserialize monitoring message.");
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MonitoringUserServiceDbContext>();

                    var clientApp = await dbContext.ClientApps
                        .FirstOrDefaultAsync(c => c.UserEmail.ToLower() == message.UserEmail.ToLower() &&
                              c.DeviceName.ToLower() == message.DeviceName.ToLower(), stoppingToken);

                    if (clientApp == null)
                    {
                        Console.WriteLine($"⚠️ No ClientApp found for email: {message.UserEmail}");
                        return;
                    }

                    var monitoredApp = await dbContext.MonitoredApps
                        .FirstOrDefaultAsync(x =>
                            x.ClientAppId == clientApp.Id &&
                            x.Name.ToLower() == message.AppName.ToLower(),
                            stoppingToken);

                    if (monitoredApp == null)
                    {
                        monitoredApp = new MonitoredApp
                        {
                            Id = Guid.NewGuid(),
                            Name = message.AppName,
                            ClientAppId = clientApp.Id,
                            IsRunning = true
                        };
                        dbContext.MonitoredApps.Add(monitoredApp);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }

                    var entity = new MonitoredAppStatus
                    {
                        MonitoredAppId = monitoredApp.Id,
                        CpuUsagePercent = message.CpuUsagePercent,
                        MemoryUsagePercent = message.MemoryUsagePercent,
                        LastStarted = DateTime.UtcNow,
                        Timestamp = DateTime.UtcNow
                    };
                    {
                        if (await dbContext.MonitoredAppStatuses.AnyAsync(x => x.MonitoredAppId == clientApp.Id))
                        {
                            dbContext.MonitoredAppStatuses.Update(entity);
                        }
                        else
                        {
                            dbContext.MonitoredAppStatuses.Add(entity);
                        }
                        await dbContext.SaveChangesAsync(stoppingToken);

                        Console.WriteLine($"Data saved for {message.DeviceName} - {message.UserEmail}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex}");
                }
            };

            await _channel.BasicConsumeAsync(queue: "monitoring.apps.status.queue", autoAck: true, consumer: consumer);

            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().Wait();
            _connection?.CloseAsync().Wait();
            base.Dispose();
        }
    }
}