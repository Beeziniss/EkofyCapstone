using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Momo;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Infrastructure.DependencyInjections;
using Refit;
using Serilog;

namespace EkofyApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load environment variables from .env file
            EnvironmentVariableLoader.LoadEnvironmentVariable();

            // Add Refit
            builder.Services
                .AddRefitClient<IMomoApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(Environment.GetEnvironmentVariable("MOMO_API_URL_BASE") ?? throw new NotFoundCustomException("Base Address is not set in the environment variables")));

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

            //Log.Logger = new LoggerConfiguration()
            //    .WriteTo.Async(a => a.File(@"F:\Logs\AEM\log.txt"))
            //    .CreateLogger();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDependencyInjection();

            builder.Services.AddGraphQLServer().AddErrorFilter<GraphQLExceptionFilter>()
                .AddAuthorization().AddType<UploadType>()
                .AddMaxExecutionDepthRule(5).AddMaxAllowedFieldCycleDepthRule(50)
                .AddMongoDbFiltering().AddMongoDbSorting().AddMongoDbProjections().AddMongoDbPagingProviders()
                .AddQueryType<QueryInitialization>().AddMutationType<MutationInitialization>()
                .AddTypes();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Empty
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EkofyApp API V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("");

            app.MapControllers();

            app.MapGraphQL("/graphql");

            app.Run();
        }
    }
}
