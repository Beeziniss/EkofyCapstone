using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Api.GraphQL.Scalars;

namespace EkofyApp.Api.GraphQL;
public static class GraphQLServer
{
    public static void RegisterGraphQLServer(this IServiceCollection services)
    {
        services.AddGraphQLServer().AddErrorFilter<GraphQLExceptionFilter>()
            .AddAuthorization()
            // Nếu expose field có data type UInt32 thì cần phải bind nó vào GraphQL
            .AddType<UploadType>().AddType(new UInt32Type())
            .BindRuntimeType<uint, UInt32Type>()
            .AddMaxExecutionDepthRule(5).AddMaxAllowedFieldCycleDepthRule(50)
            .AddProjections()
            .AddMongoDbFiltering().AddMongoDbSorting().AddMongoDbPagingProviders()
            .AddQueryType<QueryInitialization>().AddMutationType<MutationInitialization>()
            .AddTypes();
    }
}
