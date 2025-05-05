using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using DataService.Data;
using RabbitMQ.Client.Events;
using DataService.Model.MonitoringModel;
using Microsoft.EntityFrameworkCore;

namespace DataService.AsyncDataServices.MonitoringServiceSubscibers
{
    public class MonitoringDataSubscriber(IServiceScopeFactory scopeFactory, IConfiguration configuration) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private IChannel _channel = null!;
        private IConnection _connection = null!;
        private IConfiguration _configuration = configuration;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("--> Starting MonitoringDataSubscriber...");
            await InitializeRabbitMQAsync();
            _ = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
            Console.WriteLine("--> MonitoringDataSubscriber started");
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

            await _channel.ExchangeDeclareAsync(exchange: "monitoring.data", type: ExchangeType.Direct);
            await _channel.QueueDeclareAsync(queue: "monitoring.update.queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
            await _channel.QueueBindAsync(queue: "monitoring.update.queue", exchange: "monitoring.data", routingKey: "monitoring.update");
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
                    var message = JsonConvert.DeserializeObject<MonitoringMessageDto>(json);
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

                    var entity = new Model.MonitoringModel.MonitoringData
                    {
                        CpuResult = new CpuResultDto { UsagePercent = message.Data.CpuResult!.UsagePercent },
                        MemoryResult = new MemoryResultDto
                        {
                            UsedPercent = message.Data.MemoryResult!.UsedPercent,
                            TotalMemoryMb = message.Data.MemoryResult.TotalMemoryMb
                        },
                        DiskResult = new DiskResultDto
                        {
                            ReadMbps = message.Data.DiskResult!.ReadMbps,
                            WriteMbps = message.Data.DiskResult.WriteMbps,
                            Device = message.Data.DiskResult.Device
                        },
                        Timestamp = DateTime.UtcNow,
                        ClientApp = clientApp
                    };

                    dbContext.MonitoringData.Add(entity);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    Console.WriteLine($"✅ Data saved for {message.DeviceName} - {message.UserEmail}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing message: {ex}");
                }
            };

            await _channel.BasicConsumeAsync(queue: "monitoring.update.queue", autoAck: true, consumer: consumer);

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