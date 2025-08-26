using AutoMapper;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using OceanView.Domain;
using OceanView.Domain.Extensions;
using OceanView.Domain.Interfaces;
using OceanView.Domain.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace OceanView.SearchService.Services
{
    public class SearchService(ILogger<SearchService> logger, IMapper mapper,
        IDistributedCache cache, IServiceScopeFactory scopeFactory, ConnectionFactory connectionFactory) : GrpsSearchService.GrpsSearchServiceBase
    {
        private readonly ILogger<SearchService> _logger = logger;
        
        private readonly IMapper _mapper = mapper;

        private readonly IDistributedCache _cache = cache;

        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        private readonly ConnectionFactory _connectionFactory = connectionFactory;

        public override async Task<SearchIdReply> Search(SearchRequest request, ServerCallContext context)
        {
            var searchId = Guid.NewGuid().ToString();
            await _cache.SetStringAsync(searchId, OceanConstants.EmptyJsonArray);

            Task.Run(() => StartSearch(searchId, request)).Forget();
            return new SearchIdReply { Id = searchId };
        }

        public override async Task<HotelsReply> Get(GetRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Received Get request with ID: {Id}", request.Id);
            var result = await _cache.GetStringAsync(request.Id);

            var reply = new HotelsReply();
            if (!string.IsNullOrEmpty(result))
            {
                reply.Hotels.AddRange(JsonSerializer.Deserialize<Hotel[]>(result));
                _logger.LogInformation("Returning {Count} hotels for search ID: {Id}", reply.Hotels.Count, request.Id);
            }

            return reply;
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
