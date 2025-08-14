using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Api.GraphQL.Scalars;
using HotChocolate.Types.Pagination;
using StackExchange.Redis;

namespace EkofyApp.Api.GraphQL;
public static class GraphQLServer
{
    public static void RegisterGraphQLServer(this IServiceCollection services)
    {
        services.AddGraphQLServer()
            .ModifyOptions(o =>
            {
                o.StrictValidation = true;
            })
            .AddErrorFilter<GraphQLExceptionFilter>()
            .AddAuthorization()

            // Disable introspection
            .DisableIntrospection(false)

            // Performance optimizations
            // Có liên quan đến Introspection, nếu rule thấp thì không auto-fetch được schema
            .AddMaxExecutionDepthRule(20)
            .AddMaxAllowedFieldCycleDepthRule(50)
            .AddCostAnalyzer()  // Analyze query cost

            // Caching
            //.UsePersistedQueryPipeline()
            //.AddRedisQueryStorage(sp =>
            //{
            //    string config = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING_N0_SSL")!;
            //    return ConnectionMultiplexer.Connect(config);
            //})

            // Configuration
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddQueryableCursorPagingProvider(defaultProvider: false)
            .AddQueryableOffsetPagingProvider(defaultProvider: true)
            .AddPagingArguments()

            // MongoDB integration
            //.AddMongoDbProjections()
            //.AddMongoDbFiltering()
            //.AddMongoDbSorting()
            //.AddMongoDbPagingProviders()

            // Schema
            .AddQueryType<QueryInitialization>()
            .AddMutationType<MutationInitialization>()
            .AddTypes()

            // Custom scalars
            .AddType<UploadType>()
            //.AddType(new UInt32Type())
            .AddType<UInt32Type>()
            .AddType<EntitlementValueScalar>()
            .BindRuntimeType<uint, UInt32Type>();
    }
}
