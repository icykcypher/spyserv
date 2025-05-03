using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using DataService.Model.UsersModel;
using DataService.StorageRepositories;
using DataService.AsyncDataServices.UserServiceSubscribers;

public class CreateUserSubscriberService(
    IConfiguration configuration, 
    ILogger<CreateUserSubscriberService> logger, 
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<CreateUserSubscriberService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private IConnection _connection = null!;
    private IChannel _channel = null!;
    private readonly string _queueName = "user-create-queue";
    private const string ExchangeName = "user.direct";
    private const string RoutingKey = "user.created";

    private AsyncEventingBasicConsumer? _consumer;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("--> Starting CreateUserSubscriberService...");
        await InitializeRabbitMQAsync();

        _ = ExecuteAsync(cancellationToken);

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

                if (_consumer == null)
                {
                    _consumer = new AsyncEventingBasicConsumer(_channel!);
                    _consumer.ReceivedAsync += OnMessageReceived;
                    await _channel!.BasicConsumeAsync(
                        queue: _queueName,
                        autoAck: false,
                        consumer: _consumer,
                        cancellationToken: stoppingToken);
                }

                await Task.Delay(10, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in consumer loop");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task InitializeRabbitMQAsync()
    {
        try
        {
            if (_connection != null && _connection.IsOpen) return;

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

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, passive: false, autoDelete: false);
            var queue = await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);

            Console.WriteLine($"Queue status: Messages={queue.MessageCount}, Consumers={queue.ConsumerCount}");
            await _channel.QueueBindAsync(_queueName, ExchangeName, RoutingKey);

            Console.WriteLine($"Queue {0} declared and bound to exchange {1} with routing key {2}",
                _queueName, ExchangeName, RoutingKey);

            _connection.ConnectionShutdownAsync += async (s, e) =>
            {
                _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
                await Task.CompletedTask;
            };

            _logger.LogInformation("RabbitMQ initialized and listening on queue: {QueueName}", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ");
            throw;
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        Console.WriteLine("--> Received message!");
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var user = JsonConvert.DeserializeObject<User>(json);

            if (user is null)
            {
                _logger.LogWarning("Received null or invalid user object.");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            user = await ProcessMessageAsync(user);

            if (!string.IsNullOrWhiteSpace(ea.BasicProperties?.ReplyTo))
            {
                var response = new UserCreationResponse { UserId = user.Id };
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
                Console.WriteLine("Sent response back to {0} with CorrelationId {1}",
                    ea.BasicProperties.ReplyTo, replyProps.CorrelationId);
            }

            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process message: {ex}");
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
            user = await repository.AddNewUserAsync(user) ?? throw new NullReferenceException();
            _logger.LogInformation("User created successfully: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding new user to repository");
        }

        return user!;
    }

    public override void Dispose()
    {
        _consumer!.ReceivedAsync -= OnMessageReceived; 
        _channel?.CloseAsync().Wait();
        _connection?.CloseAsync().Wait();
        base.Dispose();
    }
}