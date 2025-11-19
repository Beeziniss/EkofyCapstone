namespace EkofyApp.Api.GraphQL.Mutation.PopularityMetrics;

public sealed class PopularityMetricMutationExtension : ObjectTypeExtension<PopularityMetricMutation>
{
    protected override void Configure(IObjectTypeDescriptor<PopularityMetricMutation> descriptor)
    {
        descriptor.Field(x => x.ProcessTrackStreamingMetricAsync(default!, default!))
            .AllowAnonymous();

        descriptor.Field(x => x.ProcessTrackEngagementMetricAsync(default!, default!))
            .AllowAnonymous();

        descriptor.Field(x => x.ProcessTrackDiscoveryAsync(default!, default!))
            .AllowAnonymous();

        descriptor.Field(x => x.ProcessArtistEngagementAsync(default!, default!))
            .AllowAnonymous();

        descriptor.Field(x => x.ProcessArtistDiscoveryAsync(default!, default!))
            .AllowAnonymous();
    }
}
