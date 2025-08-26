using MediatR;

namespace OceanView.SearchService.Features.Hotels
{
    public class GetHotelQuery(string id) : IRequest<Hotel[]>
    {
        public string Id { get; } = id;
    }
}