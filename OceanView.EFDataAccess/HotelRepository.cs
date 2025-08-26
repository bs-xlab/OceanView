using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OceanView.Domain.Interfaces;
using OceanView.Domain.Models;
using OceanView.EFDataAccess.Models;

namespace OceanView.EFDataAccess
{
    public class HotelRepository(HotelContext hotelContext, ILogger<HotelRepository> logger) : IHotelRepository
    {
        private readonly HotelContext _context = hotelContext ?? throw new ArgumentNullException(nameof(hotelContext));

        private readonly ILogger<HotelRepository> _logger = logger;

        public async Task<IEnumerable<HotelDto>> GetHotelsAsync(HotelSearchCriteria searchCriteria)
        {
            try
            {
                return await _context.Hotels
                       .Where(h => string.IsNullOrEmpty(searchCriteria.Country) || h.Country == searchCriteria.Country)
                       .Where(h => string.IsNullOrEmpty(searchCriteria.State) || h.State == searchCriteria.State)
                       .Where(h => string.IsNullOrEmpty(searchCriteria.City) || h.City == searchCriteria.City)
                       .Skip(searchCriteria.Offset)
                       .Take(searchCriteria.Limit)
                       .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching hotels from the database.");
                throw;
            }
        }
    }
}
