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
            .ModifyRequestOptions(opt =>
            {
                opt.ExecutionTimeout = TimeSpan.FromMinutes(5); // Tăng từ 30s lên 5 phút
            })
            .AddErrorFilter<GraphQLExceptionFilter>()
            .AddAuthorization()

            // Validation
            .AddFairyBread()

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

            // Default Scalar Types
            .AddType<UploadType>()

            // Custom Scalar Types
            .AddType<UInt32Type>()
            .AddType<EntitlementValueScalar>()
            .BindRuntimeType<uint, UInt32Type>();
    }
}
