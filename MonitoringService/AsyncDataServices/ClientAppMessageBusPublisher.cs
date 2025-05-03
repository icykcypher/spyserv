using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MonitoringService.Dto;

namespace MonitoringService.AsyncDataServices
{
    public class ClientAppMessageBusPublisher : IDisposable
    {
        private readonly IConfiguration _configuration;
        private IConnection _connection = null!;
        private IChannel _channel = null!;
        private readonly Serilog.ILogger _logger;

        public ClientAppMessageBusPublisher(IConfiguration configuration, Serilog.ILogger logger)
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

                _channel.ExchangeDeclareAsync(exchange: "clientapp.direct", type: ExchangeType.Direct, durable: true, autoDelete: false);
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

        public async Task<Guid> SendNewClientAppAsync(ClientApp clientApp)
        {
            var correlationId = Guid.NewGuid().ToString();
            var replyQueue = await _channel.QueueDeclareAsync("", exclusive: true, autoDelete: true);
            var tcs = new TaskCompletionSource<Guid>();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (model, ea) =>
            {
                if (ea.BasicProperties?.CorrelationId == correlationId)
                {
                    var body = ea.Body.ToArray();
                    var response = JsonConvert.DeserializeObject<ClientAppCreationResponse>(Encoding.UTF8.GetString(body));
                    if (response != null)
                        tcs.TrySetResult(response.ClientAppId);
                    else
                        tcs.TrySetException(new Exception("Invalid response from service."));
                }
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(replyQueue.QueueName, true, consumer);

            var properties = new BasicProperties();
            properties.ContentType = "application/json";
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueue.QueueName;

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(clientApp));

            await _channel.BasicPublishAsync(
                exchange: "clientapp.direct",
                routingKey: "clientapp.created",
                basicProperties: properties,
                body: body,
                mandatory: true
            );

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));

            Console.WriteLine($"--> Message sent! Response: {tcs.Task.Result}");

            return completed == tcs.Task ? tcs.Task.Result : Guid.Empty;
        }

        public async Task InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQHost"]!,
                Port = int.Parse(_configuration["RabbitMQPort"]!)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync("clientapp.direct", ExchangeType.Direct, durable: true);

            Console.WriteLine("--> RabbitMQ initialized!");
        }

        public void Dispose()
        {
            _channel.CloseAsync();
            _connection.CloseAsync();
        }
    }
}