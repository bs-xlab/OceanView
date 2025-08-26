using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using OceanView.Domain.Models;
using OceanView.SearchService.Features.Hotels.CQ;
using System.Text.Json;

namespace OceanView.SearchService.Features.Hotels.Handlers
{
    public class CancelSearchCommandHandler(ILogger<CancelSearchCommandHandler> logger, IDistributedCache cache) : IRequestHandler<CancelSearchCommand, string>
    {
        private readonly ILogger<CancelSearchCommandHandler> _logger = logger;

        private readonly IDistributedCache _cache = cache;

        public async Task<string> Handle(CancelSearchCommand command, CancellationToken cancellationToken)
        {
            var result = await _cache.GetStringAsync(command.Id, cancellationToken);
            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No cached result found for ID: {Id}", command.Id);
                return command.Id;
            }
            var savedHotels = JsonSerializer.Deserialize<SavedSearchResult>(result);
            if (savedHotels != null)
            {
                savedHotels.IsSearchInterrupted = true;
                await _cache.SetStringAsync(command.Id, JsonSerializer.Serialize(savedHotels), cancellationToken);
                _logger.LogInformation("Search with ID: {Id} has been marked as interrupted.", command.Id);
            }
            else
            {
                _logger.LogWarning("Failed to deserialize cached result for ID: {Id}", command.Id);
            }
            return command.Id;
        }
    }
}
