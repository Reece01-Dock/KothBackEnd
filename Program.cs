using KothBackend.Configuration;
using KothBackend.Services;
using KothBackend.Middleware;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace KothBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Kestrel
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
                options.Limits.MinRequestBodyDataRate = null;
                options.Limits.MinResponseDataRate = null;
            });

            // Configure MongoDB
            builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
            builder.Services.AddSingleton<IMongoDbService, MongoDbService>();

            // Add request logging service
            builder.Services.AddSingleton<IRequestLogService, InMemoryRequestLogService>();

            // Add Razor Pages support
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

            // Enable static files
            app.UseStaticFiles();

            // Add the request logging middleware
            app.UseRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseWebSockets();
            app.UseAuthorization();

            // Map both controllers and Razor Pages
            app.MapControllers();
            app.MapRazorPages();

            Console.WriteLine("Starting KOTH Backend on http://0.0.0.0:8000");
            app.Run("http://0.0.0.0:8000");
        }
    }
}