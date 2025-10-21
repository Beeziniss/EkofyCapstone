using EkofyApp.Api.Filters;
using EkofyApp.Api.GraphQL.Mutation;
using EkofyApp.Api.GraphQL.Query;
using EkofyApp.Api.GraphQL.Query.ApprovalHistories;
using EkofyApp.Api.GraphQL.Scalars;
using EkofyApp.Api.GraphQL.SubscriptionQL;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Enums.Artist;
using EkofyApp.Domain.Enums.BillingPortalConfig;
using EkofyApp.Domain.Enums.Coupons;
using EkofyApp.Domain.Enums.Reports;
using EkofyApp.Domain.Enums.Subcriptions;
using EkofyApp.Domain.Enums.Users;
using EkofyApp.Domain.Exceptions;
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
            .ModifyCostOptions(opt =>
            {
                opt.MaxFieldCost = 100000;
                opt.MaxTypeCost = 1000000;
            })
            .AddErrorFilter<GraphQLExceptionFilter>()
            .AddAuthorization()

            // Validation
            .AddFairyBread()

            // Disable introspection
            .DisableIntrospection(false)

            // Performance optimizations
            // Có liên quan đến Introspection, nếu rule thấp thì không auto-fetch được schema
            .AddMaxExecutionDepthRule(100)
            .AddMaxAllowedFieldCycleDepthRule(100)
            .AddCostAnalyzer()  // Analyze query cost

            // Caching
            .AddRedisSubscriptions(sp =>
            {
                var config = new ConfigurationOptions
                {
                    EndPoints = { Environment.GetEnvironmentVariable("REDIS_PUBLIC_ENDPOINT")! },
                    User = Environment.GetEnvironmentVariable("REDIS_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
                    Ssl = false,
                    AbortOnConnectFail = false
                };
                return ConnectionMultiplexer.Connect(config);
            })
            //.UsePersistedQueryPipeline()
            //.AddRedisQueryStorage(sp =>
            //{
            //    string config = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING_N0_SSL")!;
            //    return ConnectionMultiplexer.Connect(config);
            //})

            // Configuration
            .AddProjections()
            .AddFiltering()

            // Sorting
            .AddSorting()
            .AddType<EnumType<ArtistRole>>()
            .AddType<EnumType<ArtistType>>()
            .AddType<EnumType<BillingPortalConfigStatus>>()
            .AddType<EnumType<CustomerUpdate>>()
            .AddType<EnumType<StripeSubscriptionCancelMode>>()
            .AddType<EnumType<StripeSubscriptionUpdate>>()
            .AddType<EnumType<CouponDurationType>>()
            .AddType<EnumType<CouponPurposeType>>()
            .AddType<EnumType<CouponStatus>>()
            .AddType<EnumType<SubscriptionCycle>>()
            .AddType<EnumType<SubscriptionStatus>>()
            .AddType<EnumType<SubscriptionTier>>()
            .AddType<EnumType<UserGender>>()
            .AddType<EnumType<UserRole>>()
            .AddType<EnumType<UserStatus>>()
            .AddType<EnumType<AggregationLevel>>()
            .AddType<EnumType<AlbumType>>()
            .AddType<EnumType<AudioFormat>>()
            .AddType<EnumType<CategoryType>>()
            .AddType<EnumType<CurrencyType>>()
            .AddType<EnumType<DocumentType>>()
            .AddType<EnumType<EntitlementValueType>>()
            .AddType<EnumType<ImageTag>>()
            .AddType<EnumType<KeyTag>>()
            .AddType<EnumType<MoodType>>()
            .AddType<EnumType<PathTag>>()
            .AddType<EnumType<PaymentMethodType>>()
            .AddType<EnumType<PaymentStatus>>()
            .AddType<EnumType<PeriodTime>>()
            .AddType<EnumType<PolicyStatus>>()
            .AddType<EnumType<PolicyType>>()
            .AddType<EnumType<RecordingStatus>>()
            .AddType<EnumType<ReleaseStatus>>()
            .AddType<EnumType<RestrictionType>>()
            .AddType<EnumType<TrackType>>()
            .AddType<EnumType<TransactionStatus>>()
            .AddType<EnumType<WorkStatus>>()
            .AddType<EnumType<ReportStatus>>()
            .AddType<EnumType<ReportType>>()
            .AddType<EnumType<ReportAction>>()
            .AddType<EnumType<ReportPriority>>()
            .AddType<EnumType<ReportRelatedContentType>>()
            .AddType<EnumType<CommentType>>()
            .AddType<EnumType<ApprovalType>>()
            .AddType<EnumType<HistoryActionType>>()
            .AddType<EnumType<ArtistPackageStatus>>()

            // Paging
            //.AddQueryableCursorPagingProvider(defaultProvider: true)
            //.AddQueryableOffsetPagingProvider(defaultProvider: true)
            //.AddCursorPagingProvider<CursorPagingQueryableExtensions>(defaultProvider: true)
            //CursorPagingQueryableExtensions.ApplyCursorPaginationAsync();
            //.AddOffsetPagingProvider<OffsetPagingQueryableExtensions>(defaultProvider: true)
            //.AddCursorPagingProvider<QueryableCursorPagingProvider>(defaultProvider: true)
            //.AddMongoDbPagingProviders(defaultProvider: true)
            //.AddPagingArguments()

            // MongoDB integration
            //.AddMongoDbProjections()
            //.AddMongoDbFiltering()
            //.AddMongoDbSorting()
            //.AddMongoDbPagingProviders()

            // Schema
            .AddQueryType<QueryInitialization>()
            .AddMutationType<MutationInitialization>()
            .AddSubscriptionType<SubscriptionInitialization>()
            .AddTypes()

            // Default Scalar Types
            .AddType<UploadType>()

            // Custom Scalar Types
            .AddType<UInt32Type>()
            .AddType<EntitlementValueScalar>()
            .BindRuntimeType<uint, UInt32Type>();
    }
}
