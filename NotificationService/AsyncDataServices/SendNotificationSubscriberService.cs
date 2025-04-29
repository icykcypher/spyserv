using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NotificationService.Dto;
using NotificationService.Services;

namespace NotificationService.AsyncDataServices
{
    public class SendNotificationSubscriberService
        (IConfiguration configuration, ILogger<NotificationManagerService> logger, IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<NotificationManagerService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;
        private string _queueName = string.Empty;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("--> Starting SendNotificationSubscriberService...");
            await InitializeRabbitMQ();

            _ = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);

            Console.WriteLine("--> SendNotificationSubscriberService started successfully");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (_channel == null)
            {
                _logger.LogError("RabbitMQ channel is not initialized.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var notificationMessage = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Event Received: {NotificationMessage}", notificationMessage);
                    Console.WriteLine($"Event Received: {notificationMessage}");
                    var mailData = JsonConvert.DeserializeObject<MailData>(notificationMessage) ?? throw new Exception($"Cannot deserialize mail at: {nameof(SendNotificationSubscriberService)}");

                    await Task.Run(() => ProcessMessageAsync(mailData), stoppingToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    _logger.LogError("Error processing message: {ErrorMessage}", ex.Message);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        }

        private async Task InitializeRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQHost"]!,
                    Port = int.Parse(_configuration["RabbitMQPort"]!)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync("notification.direct", ExchangeType.Direct);

                _queueName = "notification_service_queue";
                await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);
                await _channel.QueueBindAsync(_queueName, "notification.direct", "send-email");

                Console.WriteLine("Listening on queue '{0}' for 'send-email' routing key...", _queueName);
                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
            }
            catch (Exception e)
            {
                _logger.LogError("Error initializing RabbitMQ: {ErrorMessage}", e.Message);
                throw;
            }
        }


        private async Task ProcessMessageAsync(MailData message)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationManagerService>();

            try
            {
                await repository.SendMail(message);
                _logger.LogInformation("Successfully processed message: AddNewUserAsync.");
            }
            catch (Exception e)
            {
                _logger.LogError("Error processing message: {ErrorMessage}", e.Message);
            }
        }

        private Task RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            _logger.LogWarning("Connection Shutdown");
            Console.WriteLine("--> Connection Shutdown");
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().Wait();
            _connection?.CloseAsync().Wait();
            base.Dispose();
        }
    }
}