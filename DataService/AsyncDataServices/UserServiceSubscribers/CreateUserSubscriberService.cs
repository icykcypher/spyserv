using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DataService.Model.UsersModel;
using DataService.StorageRepositories;

namespace DataService.AsyncDataServices.UserServiceSubscribers
{
    public class CreateUserSubscriberService(
        IConfiguration configuration,
        ILogger<CreateUserSubscriberService> logger,
        IServiceProvider serviceProvider
    ) : BackgroundService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<CreateUserSubscriberService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private IConnection _connection = null!;
        private IChannel _channel = null!;
        private readonly string _queueName = "user-create-queue";
        private const string ExchangeName = "user.direct";
        private const string RoutingKey = "user.created";

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("--> Starting CreateUserSubscriberService...");
            await InitializeRabbitMQAsync();

            _ = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);

            Console.WriteLine("--> CreateUserSubscriberService started successfully");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_channel == null || _channel.IsClosed)
                    {
                        await InitializeRabbitMQAsync();
                    }

                    var consumer = new AsyncEventingBasicConsumer(_channel!);
                    consumer.ReceivedAsync += OnMessageReceived;

                    await _channel!.BasicConsumeAsync(
                        queue: _queueName,
                        autoAck: false,
                        consumer: consumer,
                        cancellationToken: stoppingToken);

                    await Task.Delay(100, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in consumer loop: {ex.Message}");
                    await Task.Delay(10000, stoppingToken); 
                }
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQHost"]!,
                    Port = int.Parse(_configuration["RabbitMQPort"]!),
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                Console.WriteLine($"Connecting to RabbitMQ at {factory.HostName}:{factory.Port}...");
                _connection = await factory.CreateConnectionAsync();
                Console.WriteLine("RabbitMQ connection established");


                _channel = await _connection.CreateChannelAsync();
                Console.WriteLine("Channel created");

                await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, passive: false, autoDelete: false);
                var queue = await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);

                Console.WriteLine($"Queue status: Messages={queue.MessageCount}, Consumers={queue.ConsumerCount}");

                await _channel.QueueBindAsync(_queueName, ExchangeName, RoutingKey);

                Console.WriteLine("Queue {0} declared and bound to exchange {1} with routing key {2}",
                        _queueName, ExchangeName, RoutingKey);

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
                Console.WriteLine("Failed to initialize RabbitMQ " + ex.ToString());
                throw;
            }
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation("--> Received message!");
            Console.WriteLine("--> Received message!");
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var user = JsonConvert.DeserializeObject<User>(json);

                if (user is null)
                {
                    _logger.LogWarning("Received null or invalid user object.");
                    Console.WriteLine("--> Received null or invalid user object.");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                user = await ProcessMessageAsync(user);

                if (!string.IsNullOrWhiteSpace(ea.BasicProperties?.ReplyTo))
                {
                    var response = new UserCreationResponse
                    {
                        UserId = user.Id
                    };

                    var responseJson = JsonConvert.SerializeObject(response);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                    var replyProps = new BasicProperties
                    {
                        CorrelationId = ea.BasicProperties.CorrelationId
                    };

                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: ea.BasicProperties.ReplyTo!,
                        mandatory: false,
                        basicProperties: replyProps,
                        body: responseBytes
                    );

                    _logger.LogInformation("Sent response back to {ReplyTo} with CorrelationId {CorrelationId}",
                        ea.BasicProperties.ReplyTo, replyProps.CorrelationId);

                    Console.WriteLine("Sent response back to {0} with CorrelationId {1}",
                        ea.BasicProperties.ReplyTo, replyProps.CorrelationId);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
                Console.WriteLine("--> Failed to process message " + ex.ToString());
                if (_channel != null)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }

        private async Task<User> ProcessMessageAsync(User user)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserStorageRepository>();

            try
            {
                user = await repository.AddNewUserAsync(user);
                Console.WriteLine($"User created successfully: {user!.Id}"); 
                _logger.LogInformation("User created successfully: {UserId}", user.Id); 
            }
            catch (Exception ex)
            {
                Console.WriteLine("--> Error adding new user to repository " + ex.ToString());
                _logger.LogError(ex, "Error adding new user to repository");
            }

            return user!;
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().Wait();
            _connection?.CloseAsync().Wait();
            base.Dispose();
        }
    }
}