using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DataService.Model.UsersModel;
using DataService.StorageRepositories;

namespace DataService.AsyncDataServices.UserServiceSubscribers
{
    public class CreateUserSubscriberService(IConfiguration configuration, ILogger<CreateUserSubscriberService> logger,
            IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<CreateUserSubscriberService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;
        private string _queueName = string.Empty;

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeRabbitMQ();
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

                    var user = JsonConvert.DeserializeObject<User>(notificationMessage) ?? throw new Exception("Cannot deserialize user at: CreateUserSubscriberService");

                    await Task.Run(() => ProcessMessageAsync(user), stoppingToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing message: {ErrorMessage}", ex.Message);
                    _logger.LogError("Error processing message: {ErrorMessage}", ex.Message);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
        }

        private async Task InitializeRabbitMQ()
        {
            try
            {
                #pragma warning disable CS8601, CS8604 // Possible null reference assignment.
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQHost"],
                    Port = int.Parse(_configuration["RabbitMQPort"])
                };
                #pragma warning restore CS8601, CS8604 // Possible null reference assignment.

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync("trigger", ExchangeType.Fanout);
                _queueName = _channel.QueueDeclareAsync().Result.QueueName;
                await _channel.QueueBindAsync(_queueName, "trigger", "");

                Console.WriteLine($"--> Listening on the Message Bus... queue: {_queueName}");
                _logger.LogInformation("Listening on the Message Bus...");

                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
            }
            catch (Exception e)
            {
                _logger.LogError("Error initializing RabbitMQ: {ErrorMessage}", e.Message);
                throw;
            }
        }

        private async Task ProcessMessageAsync(User message)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserStorageRepository>();

            try
            {
                await repository.AddNewUserAsync(message);
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