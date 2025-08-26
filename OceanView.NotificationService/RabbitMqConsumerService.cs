using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OceanView.NotificationService
{
    public class RabbitMqConsumerService(ILogger<RabbitMqConsumerService> logger, IConfiguration configuration) : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumerService> _logger = logger;

        private readonly IConfiguration _configuration = configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rabbitMqConnectionString = _configuration.GetConnectionString("RabbitMQ")
                ?? throw new Exception("GetConnectionString RabbitMQ is not found");

            var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };

            using var connection = await factory.CreateConnectionAsync(stoppingToken);
            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync("search_completed", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Message received: {Message}", message);
                return Task.FromResult(ea);
            };

            await channel.BasicConsumeAsync("search_completed", autoAck: false, consumer, cancellationToken: stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

}
