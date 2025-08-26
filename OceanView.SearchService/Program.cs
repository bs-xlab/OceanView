using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OceanView.Domain.Interfaces;
using OceanView.Domain.Models;
using OceanView.EFDataAccess;
using OceanView.EFDataAccess.Models;
using RabbitMQ.Client;

namespace OceanView.SearchService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddGrpc();
            builder.Services.AddDbContext<HotelContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
            });
            builder.Services.AddDistributedRedisCache(option =>
            {
                option.Configuration = builder.Configuration.GetConnectionString("Redis");
            });
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
            builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
            builder.Services.AddScoped<IHotelRepository, HotelRepository>();
            builder.Services.AddSingleton(sp =>
            {
                var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
                    ?? throw new Exception("GetConnectionString RabbitMQ is not found");
                return new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
            });

            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<HotelDto, Hotel>();
                cfg.CreateMap<HotelDto, HotelInfo>();
                cfg.CreateMap<HotelInfo, Hotel>();
            });

            builder.Configuration.AddEnvironmentVariables();
            var app = builder.Build();

            app.MapGrpcService<Services.SearchService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

            app.Run();
        }
    }
}