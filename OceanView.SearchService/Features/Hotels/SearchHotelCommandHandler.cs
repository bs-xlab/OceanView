using AutoMapper;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using OceanView.Domain;
using OceanView.Domain.Extensions;
using OceanView.Domain.Interfaces;
using OceanView.Domain.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace OceanView.SearchService.Features.Hotels
{
    public class SearchHotelCommandHandler(ILogger<SearchHotelCommandHandler> logger, IMapper mapper,
        IDistributedCache cache, IServiceScopeFactory scopeFactory, ConnectionFactory connectionFactory) : IRequestHandler<SearchHotelCommand, string>
    {
        private readonly ILogger<SearchHotelCommandHandler> _logger = logger;

        private readonly IMapper _mapper = mapper;

        private readonly IDistributedCache _cache = cache;

        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        private readonly ConnectionFactory _connectionFactory = connectionFactory;

        public async Task<string> Handle(SearchHotelCommand command, CancellationToken cancellationToken)
        {
            var searchId = Guid.NewGuid().ToString();
            await _cache.SetStringAsync(searchId, OceanConstants.EmptyJsonArray, cancellationToken);

            Task.Run(() => StartSearch(searchId, command.Request), cancellationToken).Forget();
            return searchId;
        }

        private async Task StartSearch(string searchId, SearchRequest request)
        {
            int batchSize = 5;
            int offset = 0;

            using var scope = _scopeFactory.CreateScope();
            var hotelRepo = scope.ServiceProvider.GetRequiredService<IHotelRepository>();

            _logger.LogInformation("Starting search with ID: {SearchId} for {City}, {State}, {Country}",
                searchId, request.City, request.State, request.Country);

            while (true)
            {
                var hotelsDto = await hotelRepo.GetHotelsAsync(new HotelSearchCriteria(
                    offset, batchSize, request.City, request.State, request.Country));

                var hotels = _mapper
                    .Map<IEnumerable<HotelDto>, IEnumerable<Hotel>>(hotelsDto)
                    .ToList();

                if (hotels.Count == 0) break;
                var savedResult = await _cache.GetStringAsync(searchId);

                if (string.IsNullOrEmpty(savedResult))
                {
                    savedResult = OceanConstants.EmptyJsonArray;
                    await _cache.SetStringAsync(searchId, savedResult);
                }

                var hotelsReply = new HotelsReply();
                var hotelsResult = JsonSerializer.Deserialize<Hotel[]>(savedResult);

                if (hotelsResult != null)
                {
                    hotelsReply.Hotels.AddRange(hotelsResult);
                }

                hotelsReply.Hotels.AddRange(hotels);

                await _cache.SetStringAsync(searchId, JsonSerializer.Serialize(hotelsReply.Hotels.ToArray()));
                offset += batchSize;

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            var brokerConnection = await _connectionFactory.CreateConnectionAsync();

            _logger.LogInformation("Search with ID: {SearchId} completed. Collection count: {Offset}", searchId, offset);
            var channel = await brokerConnection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "search_completed",
                durable: true, exclusive: false,
                autoDelete: false, arguments: null);

            await channel.BasicPublishAsync(exchange: "",
                routingKey: "search_completed",
                body: System.Text.Encoding.UTF8.GetBytes(searchId));
        }
    }
}
