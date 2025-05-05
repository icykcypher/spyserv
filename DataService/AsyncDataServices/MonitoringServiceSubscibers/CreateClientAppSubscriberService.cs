using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DataService.StorageRepositories;
using DataService.Model.MonitoringModel;

namespace DataService.AsyncDataServices.ClientAppServiceSubscribers
{
    public class CreateClientAppSubscriberService(
        IConfiguration configuration,
        ILogger<CreateClientAppSubscriberService> logger,
        IServiceProvider serviceProvider
    ) : BackgroundService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<CreateClientAppSubscriberService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private IConnection _connection = null!;
        private IChannel _channel = null!;
        private const string ExchangeName = "clientapp.direct";
        private const string RoutingKey = "clientapp.created";
        private const string QueueName = "clientapp-create-queue";

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("--> Starting CreateClientAppSubscriberService...");
            await InitializeRabbitMQAsync();
            _ = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
            Console.WriteLine("--> CreateClientAppSubscriberService started");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceived;

            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
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
            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);

            _connection.ConnectionShutdownAsync += async (s, e) =>
            {
                _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
                await Task.CompletedTask;
            };
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var clientApp = JsonConvert.DeserializeObject<ClientApp>(json);

                if (clientApp == null)
                {
                    _logger.LogWarning("Received null ClientApp");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    return;
                }

                clientApp = await ProcessClientAppAsync(clientApp);

                if (!string.IsNullOrWhiteSpace(ea.BasicProperties?.ReplyTo))
                {
                    var response = new ClientAppCreationResponse
                    {
                        ClientAppId = clientApp.Id
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

                    Console.WriteLine("--> Sent response for ClientApp ID: {0}", clientApp.Id);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ClientApp message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }

        private async Task<ClientApp> ProcessClientAppAsync(ClientApp clientApp)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMonitoringStorageRepository>();

            try
            {
                clientApp = await repository.AddNewClientAppAsync(clientApp);
                _logger.LogInformation("ClientApp created successfully: {ClientAppId}", clientApp.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _logger.LogError(ex, "Error saving ClientApp to repository");
            }

            return clientApp!;
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().Wait();
            _connection?.CloseAsync().Wait();
            base.Dispose();
        }
    }
}