using Serilog;
using EkofyApp.Api.Filters;
using EkofyApp.Infrastructure.DependencyInjections;

namespace EkofyApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load environment variables from .env file
            EnvironmentVariableLoader.LoadEnvironmentVariable();

            // Add services to the container.
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<BaseExceptionFilter>();
            });

            // Register Serilog 
            builder.Host.UseSerilog((hostingContext, LoggerConfiguration) =>
            {
                LoggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration);
            });

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(@"F:\Logs\AEM\log.txt"))
                .CreateLogger();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDependencyInjection();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCors("");

            app.MapControllers();

            app.Run();
        }
    }
}
