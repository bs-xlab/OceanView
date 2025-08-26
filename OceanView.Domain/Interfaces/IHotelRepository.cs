using OceanView.Domain.Models;

namespace OceanView.Domain.Interfaces
{
    public interface IHotelRepository
    {
        public Task<IEnumerable<HotelDto>> GetHotelsAsync(HotelSearchCriteria searchCriteria);
    }
}
