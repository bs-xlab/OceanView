using MediatR;

namespace OceanView.SearchService.Features.Hotels.CQ
{
    public class SearchHotelCommand(SearchRequest request) : IRequest<string>
    {
        public SearchRequest Request { get; } = request;
    }
}
