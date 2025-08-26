using Microsoft.AspNetCore.Mvc;
using OceanView.Domain.Models;

namespace OceanView.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddAuthorization();
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddCors();

            builder.Services.AddGrpcClient<GrpsSearchService.GrpsSearchServiceClient>(options =>
            {
                var searchServiceUrl = builder.Configuration["Grpc:SearchServiceUrl"];
                if (string.IsNullOrWhiteSpace(searchServiceUrl))
                    throw new InvalidOperationException("Configuration value 'Grpc:SearchServiceUrl' is missing or empty.");
                options.Address = new Uri(searchServiceUrl);
            });

            var app = builder.Build();
            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.UseCors(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());

            app.MapPost("/search", async (HttpContext httpContext, GrpsSearchService.GrpsSearchServiceClient greeterClient, [FromBody] HotelSearchCriteria criteria) =>
            {
                var reply = await greeterClient.SearchAsync(
                    new SearchRequest { Country = criteria.Country, City = criteria.City, State = criteria.State });

                return reply;
            });

            app.MapGet("/get", async (HttpContext httpContext, GrpsSearchService.GrpsSearchServiceClient searchClient, [FromQuery] string id) =>
            {
                var hotelsReply = await searchClient.GetAsync(new IdRequest { Id = id });
                return hotelsReply;
            });

            app.MapPost("/cancelSearch", async (HttpContext httpContext, GrpsSearchService.GrpsSearchServiceClient searchClient, [FromBody] string id) =>
            {
                var hotelsReply = await searchClient.CancelSearchAsync(new IdRequest { Id = id });
                return hotelsReply;
            });

            app.MapGet("/launch", () => "OceanView Web API is running. Use /search or /get endpoints to interact with the service.");
            app.Run();
        }
    }
}
