using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using OceanView.Domain.Models;
using OceanView.SearchService.Features.Hotels.CQ;
using System.Text.Json;

namespace OceanView.SearchService.Features.Hotels.Handlers
{
    public class GetHotelQueryHandler(ILogger<GetHotelQueryHandler> logger, IDistributedCache cache, IMapper mapper) : IRequestHandler<GetHotelQuery, SavedSearchResult>
    {
        private readonly ILogger<GetHotelQueryHandler> _logger = logger;

        private readonly IDistributedCache _cache = cache;

        private readonly IMapper _mapper = mapper;

        public async Task<SavedSearchResult> Handle(GetHotelQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetHotelQuery for ID: {Id}", query.Id);
            var result = await _cache.GetStringAsync(query.Id, cancellationToken);
            
            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No cached result found for ID: {Id}", query.Id);
                return new SavedSearchResult();
            }

            return JsonSerializer.Deserialize<SavedSearchResult>(result) ?? new SavedSearchResult();
        }
    }
}