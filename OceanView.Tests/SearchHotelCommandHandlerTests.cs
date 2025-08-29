using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OceanView.Domain.Interfaces;
using OceanView.Domain.Models;
using OceanView.SearchService;
using OceanView.SearchService.Features.Hotels.CQ;
using OceanView.SearchService.Features.Hotels.Handlers;
using System.Text;
using System.Text.Json;

public class SearchHotelCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSearchId_AndStartsSearch()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SearchHotelCommandHandler>>();
        var mapperMock = new Mock<IMapper>();
        var cacheMock = new CacheMock();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        mapperMock.Setup(m => m.Map<IEnumerable<HotelDto>, IEnumerable<HotelInfo>>(It.IsAny<IEnumerable<HotelDto>>()))
            .Returns((IEnumerable<HotelDto> dtos) => dtos.Select(d => new HotelInfo { Country = d.Country }));

        var hotels = new List<HotelDto>
        {
            new HotelDto { Id = 1, Country = "Panama" },
            new HotelDto { Id = 2, Country = "Panama" },
            new HotelDto { Id = 3, Country = "Panama" }
        };

        // Arrange
        var repoMock = new Mock<IHotelRepository>();
        repoMock.Setup(r => r.GetHotelsAsync(new HotelSearchCriteria(0, 5, "", "", "Panama")))
            .Returns(Task.FromResult(hotels.AsEnumerable()));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IHotelRepository)))
            .Returns(repoMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        scopeFactoryMock.Setup(sf => sf.CreateScope())
            .Returns(() => scopeMock.Object);

        var handler = new SearchHotelCommandHandler(
            loggerMock.Object,
            mapperMock.Object,
            cacheMock,
            scopeFactoryMock.Object);

        var command = new SearchHotelCommand(new SearchRequest() { Country = "Panama", Limit = 5});

        // Act
        var id = await handler.Handle(command, CancellationToken.None);

        // Wait for the background search to complete
        await Task.Delay(7000);

        // Assert
        Assert.False(string.IsNullOrEmpty(id));
        Assert.True(Guid.TryParse(id, out Guid guid));
        Assert.True(JsonSerializer.Deserialize<SavedSearchResult>(cacheMock.GetString(id)).Hotels.Count == hotels.Count);
    }

    public class CacheMock : IDistributedCache
    {
        private string _cache = "{}";

        public byte[]? Get(string key)
        {
            return Encoding.UTF8.GetBytes(_cache);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(_cache));
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _cache = Encoding.UTF8.GetString(value);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            _cache = Encoding.UTF8.GetString(value);
            return Task.CompletedTask;
        }
    }
}