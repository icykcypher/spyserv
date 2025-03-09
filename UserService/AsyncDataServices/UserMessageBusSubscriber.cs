using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json;
using UserService.Model;
using RabbitMQ.Client.Events;
using ILogger = Serilog.ILogger;
using UserService.Model.Requests;

namespace UserService.AsyncDataServices
{
    public class UserMessageBusSubscriber : IUserMessageBusSubscriber, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger _logger;

        public UserMessageBusSubscriber(IConfiguration configuration, ILogger logger)
        {
            this._configuration = configuration;
            this._logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"]),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            try
            {
                _connection = factory.CreateConnectionAsync().Result;
                _channel = _connection.CreateChannelAsync().Result;

                _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
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

        private async Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs @event)
        {
            Console.WriteLine($"--> RabbitMQ Connection Shutdown");
            _logger.Error($"RabbitMQ Connection Shutdown");
        }

        public async Task<Guid> SendNewUserAsync(User user)
        {
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            try
            {
                if (_connection.IsOpen)
                {
                    return await _sendNewUserAsync(user, properties);
                }
                else
                {
                    _logger.Error("RabbitMQ Connection is closed, not sending message...");
                    Console.WriteLine("--> RabbitMQ Connection is closed, not sending message...");
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error sending message: {e.Message}");
                Console.WriteLine($"--> Error sending message: {e.Message}");
            }

            return Guid.Empty; 
        }

        private async Task<Guid> _sendNewUserAsync(User user, BasicProperties properties)
        {
            var correlationId = Guid.NewGuid().ToString();
            var replyQueueName = _channel.QueueDeclareAsync("user-creation-queue").Result.QueueName;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            var tcs = new TaskCompletionSource<Guid>();

            consumer.ReceivedAsync += async (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var body = ea.Body.ToArray();
                    var response = JsonConvert.DeserializeObject<UserCreationResponse>(Encoding.UTF8.GetString(body)) 
                        ?? throw new Exception("Error in deserialization");
                    
                    tcs.SetResult(response.UserId);
                }
            };

            await _channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: consumer);

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user));

            properties.ReplyTo = replyQueueName;
            properties.CorrelationId = correlationId;

            await _channel.BasicPublishAsync
           (
               exchange: "trigger",
               routingKey: "",
               mandatory: true,
               basicProperties: properties,
               body: body
           );

            var userId = await tcs.Task;
            return userId;
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };
            try
            {
                if (_connection.IsOpen)
                {
                    await _deleteUserAsync(userId, properties);
                }
                else
                {
                    _logger.Error("RabbitMQ Connection is closed, not sending message...");
                    Console.WriteLine("--> RabbitMQ Connection is closed, not sending message...");
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error sending message: {e.Message}");
                Console.WriteLine($"--> Error sending message: {e.Message}");
            }
        }

        private async Task _deleteUserAsync(Guid userId, BasicProperties properties)
        {
            var correlationId = Guid.NewGuid().ToString();
            var replyQueueName = _channel.QueueDeclareAsync("user-deletion-queue").Result.QueueName;
            
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(userId));
            properties.ReplyTo = replyQueueName;
            properties.CorrelationId = correlationId;
            await _channel.BasicPublishAsync
           (
               exchange: "trigger",
               routingKey: "",
               mandatory: true,
               basicProperties: properties,
               body: body
           );
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
        }
    }
}