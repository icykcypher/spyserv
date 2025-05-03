using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using UserService.Model;
using RabbitMQ.Client.Events;
using UserService.Model.Requests;
using UserService.AsyncDataServices;

public class UserMessageBusSubscriber : IUserMessageBusSubscriber, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IConnection _connection = null!;
    private readonly IChannel _channel = null!;
    private readonly Serilog.ILogger _logger;
    private readonly string _replyQueueName;
    private readonly string _exchangeName = "user.direct";

    private AsyncEventingBasicConsumer _consumer = null!;

    public UserMessageBusSubscriber(IConfiguration configuration, Serilog.ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;

        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQHost"]!,
            Port = int.Parse(_configuration["RabbitMQPort"]!),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        try
        {
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;

            _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
            _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;

            Console.WriteLine("--> Connected to Message Bus");

            // Create a reply queue once and reuse it
            var replyQueue = _channel.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true).Result;
            _replyQueueName = replyQueue.QueueName;

            // Set up the consumer for the reply queue once
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _channel.BasicConsumeAsync(_replyQueueName, true, _consumer);
        }
        catch (Exception e)
        {
            Console.WriteLine($"--> Could not connect to the Message Bus: {e.Message}");
            _logger.Error($"Could not connect to the Message Bus: {e.Message}");
            throw;
        }
    }

    private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs @event)
    {
        Console.WriteLine($"--> RabbitMQ Connection Shutdown");
        _logger.Error($"RabbitMQ Connection Shutdown");
        return Task.CompletedTask;
    }

    public async Task<Guid> SendNewUserAsync(User user)
    {
        try
        {
            if (!_connection.IsOpen)
            {
                _logger.Error("RabbitMQ Connection is closed, not sending message...");
                return Guid.Empty;
            }

            var correlationId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<Guid>();

            // Setup response listener
            _consumer.ReceivedAsync += async (model, ea) =>
            {
                if (ea.BasicProperties?.CorrelationId == correlationId)
                {
                    var body = ea.Body.ToArray();
                    var response = JsonConvert.DeserializeObject<UserCreationResponse>(Encoding.UTF8.GetString(body));

                    if (response != null)
                    {
                        tcs.TrySetResult(response.UserId);
                    }
                    else
                    {
                        tcs.TrySetException(new Exception("Invalid response from DataService"));
                    }
                }
                await Task.CompletedTask;
            };

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName
            };

            var userBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user));

            // Publish the message to DataService
            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: "user.created",
                mandatory: true,
                basicProperties: properties,
                body: userBody
            );

            // Timeout handling
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(20));
            var completed = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completed == timeoutTask)
            {
                _logger.Error("Timeout waiting for response from DataService");
                return Guid.Empty;
            }

            return await tcs.Task;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending message: {e.Message}");
            _logger.Error($"Error sending message: {e.Message}");
            return Guid.Empty;
        }
    }

    public void Dispose()
    {
        Console.WriteLine($"MessageBus Disposed");

        try
        {
            if (_channel.IsOpen)
            {
                _channel.CloseAsync().Wait();
                _connection.CloseAsync().Wait();
            }

            _channel.Dispose();
            _connection.Dispose();
        }
        catch (Exception e)
        {
            _logger.Error($"Error during disposal: {e.Message}");
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    public Task<User> DeleteUserAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}