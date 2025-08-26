using AutoMapper;
using Grpc.Core;
using MediatR;
using OceanView.Domain.Models;
using OceanView.SearchService.Features.Hotels.CQ;

namespace OceanView.SearchService.Services
{
    public class SearchService(ISender sender, IMapper mapper) : GrpsSearchService.GrpsSearchServiceBase
    {
        private readonly ISender _sender = sender;

        private readonly IMapper _mapper = mapper;

        public override async Task<SearchIdReply> Search(SearchRequest request, ServerCallContext context)
        {
            var searchId = await _sender.Send(new SearchHotelCommand(request));
            return new SearchIdReply { Id = searchId };
        }

        public override async Task<HotelsReply> Get(IdRequest request, ServerCallContext context)
        {
            var hotelQuery = new GetHotelQuery(request.Id);
            var hotelResult = await _sender.Send(hotelQuery);

            HotelsReply reply = new()
            {
                IsSearchCompleted = hotelResult.IsSearchCompleted || hotelResult.IsSearchInterrupted
            };

            reply.Hotels.AddRange(_mapper.Map<IEnumerable<HotelInfo>, IEnumerable<Hotel>>(hotelResult.Hotels));
            return reply;
        }

        public override async Task<SearchIdReply> CancelSearch(IdRequest request, ServerCallContext context)
        {
            var cmd = new CancelSearchCommand(request.Id);
            var searchId = await _sender.Send(cmd);
            return new SearchIdReply { Id = searchId };
        }
    }
}
