namespace OceanView.Domain.Models
{
    public record HotelSearchCriteria(int Offset, int Limit, string? City, string? State, string? Country);
}
