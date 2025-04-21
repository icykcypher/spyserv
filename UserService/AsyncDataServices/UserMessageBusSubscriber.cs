using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using UserService.Model;
using RabbitMQ.Client.Events;
using UserService.Model.Requests;
using UserService.AsyncDataServices;

public class UserMessageBusSubscriber : IUserMessageBusSubscriber, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly Serilog.ILogger _logger;

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

            _channel.ExchangeDeclareAsync(exchange: "user.direct", type: ExchangeType.Direct, durable: true, autoDelete: false);
            _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;

            Console.WriteLine("--> Connected to Message Bus");
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
                Console.WriteLine("--> RabbitMQ Connection is closed, not sending message...");
                return Guid.Empty;
            }

            var correlationId = Guid.NewGuid().ToString();

            var replyQueue = await _channel.QueueDeclareAsync(queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true);
            var replyQueueName = replyQueue.QueueName;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            var tcs = new TaskCompletionSource<Guid>();

            consumer.ReceivedAsync += (model, ea) =>
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
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queue: replyQueueName, autoAck: true, consumer: consumer);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                CorrelationId = correlationId,
                ReplyTo = replyQueueName
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user));

            await _channel.BasicPublishAsync(
                exchange: "user.direct",
                routingKey: "user.created",
                mandatory: true,
                basicProperties: properties,
                body: messageBody
            );

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(20));
            var completed = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completed == timeoutTask)
            {
                _logger.Error("Timeout waiting for response from DataService");
                Console.WriteLine("Timeout waiting for response from DataService");
                return Guid.Empty;
            }

            return await tcs.Task;
        }
        catch (Exception e)
        {
            _logger.Error($"Error sending message: {e.Message}");
            Console.WriteLine($"--> Error sending message: {e.Message}");
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
                _channel.CloseAsync();
                _connection.CloseAsync();
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