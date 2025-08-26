using MediatR;
using OceanView.SearchService.Features.Hotels.CQ;
using RabbitMQ.Client;

namespace OceanView.SearchService.Features.Hotels.Handlers
{
    public class NotifySearchCompletedHandler(ILogger<NotifySearchCompletedHandler> logger, ConnectionFactory connectionFactory) : IRequestHandler<NotifySearchCompletedCommand, Unit>
    {
        private readonly ILogger<NotifySearchCompletedHandler> _logger = logger;

        private readonly ConnectionFactory _connectionFactory = connectionFactory;

        public async Task<Unit> Handle(NotifySearchCompletedCommand command, CancellationToken cancellationToken)
        {
            var brokerConnection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            _logger.LogInformation("Search with ID: {Id} completed. Collection count: {Count}", command.Id, command.Count);
            var channel = await brokerConnection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(queue: "search_completed", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

            await channel.BasicPublishAsync(exchange: "", routingKey: "search_completed", body: System.Text.Encoding.UTF8.GetBytes(command.Id), cancellationToken: cancellationToken);
            return Unit.Value;
        }
    }
}
