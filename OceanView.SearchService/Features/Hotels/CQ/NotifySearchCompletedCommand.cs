using MediatR;

namespace OceanView.SearchService.Features.Hotels.CQ
{
    public class NotifySearchCompletedCommand(string id, int count) : IRequest<Unit>
    {
        public string Id { get; } = id;

        public int Count { get; } = count;
    }
}