using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DataService.Model.UsersModel;
using DataService.Services.UserServices;

namespace DataService.AsyncDataServices.UserServiceSubscribers
{
    public class UpdateUserSubscriberService(
        IConfiguration configuration,
        ILogger<UpdateUserSubscriberService> logger,
        IServiceProvider serviceProvider
    ) : BackgroundService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<UpdateUserSubscriberService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private IConnection? _connection;
        private IChannel? _channel;
        private string _queueName = "user-update-queue";
        private const string _exchangeName = "user.direct";
        private const string _routingKey = "user.updated";

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeRabbitMQAsync();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ channel is not initialized.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceived;

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Started consuming messages on queue: {QueueName}", _queueName);
        }

        private async Task InitializeRabbitMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQHost"]!,
                    Port = int.Parse(_configuration["RabbitMQPort"]!)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Direct, durable: true, passive: false, autoDelete: false);
                await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);
                await _channel.QueueBindAsync(_queueName, _exchangeName, _routingKey);

                _connection.ConnectionShutdownAsync += async (s, e) =>
                {
                    _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
                    await Task.CompletedTask;
                };

                _logger.LogInformation("RabbitMQ initialized and listening on queue: {QueueName}", _queueName);
                Console.WriteLine("RabbitMQ initialized and listening on queue: {0}", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ");
                throw;
            }

            await Task.CompletedTask;
        }


        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var user = JsonConvert.DeserializeObject<User>(json);

                if (user is null)
                {
                    _logger.LogWarning("Received null or invalid user object.");
                    _channel?.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                await ProcessMessageAsync(user);
                _channel?.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
                _channel?.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }

        private async Task ProcessMessageAsync(User user)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserDatabaseService>();
            await repository.UpdateUser(user);
            _logger.LogInformation("User updated successfully: {UserId}", user.Id);
        }

        public override void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            base.Dispose();
        }
    }
}