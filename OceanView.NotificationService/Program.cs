namespace OceanView.NotificationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddAuthorization();
            builder.Services.AddHostedService<RabbitMqConsumerService>();
            builder.Configuration.AddEnvironmentVariables();

            var app = builder.Build();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.Run();
        }
    }
}
