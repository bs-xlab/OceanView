using MediatR;

namespace OceanView.SearchService.Features.Hotels.CQ
{
    public class CancelSearchCommand(string id) : IRequest<string>
    {
        public string Id { get; } = id;
    }
}