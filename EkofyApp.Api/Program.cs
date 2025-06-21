using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Mutation.Artist;
using EkofyApp.Api.GraphQL.Mutation.Authentication;
using EkofyApp.Api.GraphQL.Mutation.Test;
using EkofyApp.Api.GraphQL.Mutation.Track;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Api.GraphQL.Query.Artist;
using EkofyApp.Api.GraphQL.Query.Test;
using EkofyApp.Api.GraphQL.Query.Track;
using EkofyApp.Infrastructure.DependencyInjections;
using HotChocolate.Execution.Configuration;
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

            builder.Services.AddGraphQLServer()
                .AddAuthorization().AddType<UploadType>()
                .AddMaxExecutionDepthRule(5).AddMaxAllowedFieldCycleDepthRule(50)
                .AddMongoDbFiltering().AddMongoDbSorting().AddMongoDbProjections().AddMongoDbPagingProviders()
                //.AddDataLoader<ArtistByIdDataLoader>()
                .AddQueryType<QueryInitialization>()
                .AddTypeExtension<TestQuery>()
                .AddTypeExtension<TrackQuery>()
                .AddTypeExtension<ArtistQuery>()
                .AddTypeExtension<TracksResolver>()
                .AddMutationType<MutationInitialization>()
                .AddTypeExtension<AuthenticationMutation>()
                .AddTypeExtension<TestMutation>()
                .AddTypeExtension<TrackMutation>()
                .AddTypeExtension<ArtistMutation>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Empty
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("");

            app.MapControllers();

            app.MapGraphQL("/graphql");

            app.Run();
        }
    }
}
