using KothBackend.Configuration;
using KothBackend.Services;
using KothBackend.Middleware;
using System.Text.Json.Serialization;

namespace KothBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure MongoDB
            builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
            builder.Services.AddSingleton<IMongoDbService, MongoDbService>();

            // Add request logging service (new)
            builder.Services.AddSingleton<IRequestLogService, InMemoryRequestLogService>();

            // Add Razor Pages support (new)
            builder.Services.AddRazorPages();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Enable static files (new)
            app.UseStaticFiles();

            // Add the request logging middleware (new)
            app.UseRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Add routing middleware (new)
            app.UseRouting();

            app.UseWebSockets();
            app.UseAuthorization();

            // Map both controllers and Razor Pages (updated)
            app.MapControllers();
            app.MapRazorPages();

            Console.WriteLine("Starting KOTH Backend on http://0.0.0.0:8000");
            app.Run("http://0.0.0.0:8000");
        }
    }
}