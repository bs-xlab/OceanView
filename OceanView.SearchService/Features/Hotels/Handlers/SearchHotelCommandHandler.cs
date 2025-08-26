using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using OceanView.Domain;
using OceanView.Domain.Extensions;
using OceanView.Domain.Interfaces;
using OceanView.Domain.Models;
using OceanView.SearchService.Features.Hotels.CQ;
using RabbitMQ.Client;
using System.Text.Json;

namespace OceanView.SearchService.Features.Hotels.Handlers
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
            await _cache.SetStringAsync(searchId, OceanConstants.EmptyJsonObject, cancellationToken);

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
            
            try
            {
                while (true)
                {
                    var hotelsDto = await hotelRepo.GetHotelsAsync(new HotelSearchCriteria(
                        offset, batchSize, request.City, request.State, request.Country));

                    var hotels = _mapper
                        .Map<IEnumerable<HotelDto>, IEnumerable<HotelInfo>>(hotelsDto)
                        .ToList();

                    if (hotels.Count == 0) break;
                    var savedResult = await _cache.GetStringAsync(searchId);

                    if (string.IsNullOrEmpty(savedResult))
                    {
                        savedResult = OceanConstants.EmptyJsonObject;
                        await _cache.SetStringAsync(searchId, savedResult);
                    }

                    var hotelsResult = JsonSerializer.Deserialize<SavedSearchResult>(savedResult);

                    if (hotelsResult == null)
                    {
                        hotelsResult = new SavedSearchResult { Hotels = hotels };
                    }
                    else
                    {
                        hotelsResult.Hotels.AddRange(hotels);
                    }

                    if (hotelsResult.IsSearchInterrupted)
                    {
                        break;
                    }

                    await _cache.SetStringAsync(searchId, JsonSerializer.Serialize(hotelsResult));
                    offset += batchSize;

                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the search process for ID: {SearchId}", searchId);
                throw;
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
