using MediatR;
using OceanView.Domain.Models;

namespace OceanView.SearchService.Features.Hotels.CQ
{
    public class GetHotelQuery(string id) : IRequest<SavedSearchResult>
    {
        public string Id { get; } = id;
    }
}