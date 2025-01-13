
using KothBackend.Configuration;
using KothBackend.Services;
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

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseWebSockets();
            app.UseAuthorization();
            app.MapControllers();

            Console.WriteLine("Starting KOTH Backend on http://0.0.0.0:8000");
            app.Run("http://0.0.0.0:8000");
        }
    }
}
