using Bogus;
using OceanView.Domain.Models;

namespace OceanView.EFDataAccess
{
    public static class HotelSeeder
    {
        public static List<HotelDto> GenerateHotels(int count = 1000)
        {
            var faker = new Faker<HotelDto>()
                .RuleFor(h => h.Id, f => f.UniqueIndex)
                .RuleFor(h => h.Name, f => $"{f.Company.CompanyName()} Hotel")
                .RuleFor(h => h.City, f => f.Address.City())
                .RuleFor(h => h.Address, f => f.Address.StreetAddress())
                .RuleFor(h => h.State, f => f.Address.State())
                .RuleFor(h => h.Country, f => f.Address.Country())
                .RuleFor(h => h.Rating, f => f.Random.Float(1, 5));

            return faker.Generate(count);
        }
    }

}
