using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Api.GraphQL.Scalars;
using StackExchange.Redis;

namespace EkofyApp.Api.GraphQL;
public static class GraphQLServer
{
    public static void RegisterGraphQLServer(this IServiceCollection services)
    {
        services.AddGraphQLServer()
            .AddErrorFilter<GraphQLExceptionFilter>()
            .AddAuthorization()
            
            // Performance optimizations
            .AddMaxExecutionDepthRule(5)
            .AddMaxAllowedFieldCycleDepthRule(50)
            .AddCostAnalyzer()  // Analyze query cost

            // Caching
            //.UsePersistedQueryPipeline()
            //.AddRedisQueryStorage(sp =>
            //{
            //    string config = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING_N0_SSL")!;
            //    return ConnectionMultiplexer.Connect(config);
            //})

            // MongoDB integration
            .AddProjections()
            .AddMongoDbFiltering()
            .AddMongoDbSorting()
            .AddMongoDbPagingProviders()
            
            // Schema
            .AddQueryType<QueryInitialization>()
            .AddMutationType<MutationInitialization>()
            .AddTypes()
            
            // Custom scalars
            .AddType<UploadType>()
            .AddType(new UInt32Type())
            .BindRuntimeType<uint, UInt32Type>();
    }
}
