using Microsoft.EntityFrameworkCore;
using OceanView.Domain.Models;

namespace OceanView.EFDataAccess.Models
{
    public class HotelContext : DbContext
    {
        public DbSet<HotelDto> Hotels { get; set; } = null!;

        public HotelContext(DbContextOptions<HotelContext> options) : base(options) => Database.EnsureCreated();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var hotels = HotelSeeder.GenerateHotels(5000);

            modelBuilder.Entity<HotelDto>().HasData(hotels);

            base.OnModelCreating(modelBuilder);
        }
    }
}
