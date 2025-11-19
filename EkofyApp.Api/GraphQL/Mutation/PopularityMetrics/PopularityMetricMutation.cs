using EkofyApp.Application.ServiceInterfaces.Jobs;
using EkofyApp.Domain.Enums;
using Hangfire;

namespace EkofyApp.Api.GraphQL.Mutation.PopularityMetrics;

[ExtendObjectType<MutationInitialization>]
[MutationType]
public sealed class PopularityMetricMutation
{
    public async Task<bool> ProcessTrackStreamingMetricAsync(string trackId, PopularityActionType actionType)
    {
        BackgroundJob.Enqueue<IBackgoundService>(x => x.ProcessTrackStreamingMetricJobAsync(trackId, actionType));
        return true;
    }

    public async Task<bool> ProcessTrackEngagementMetricAsync(string trackId, PopularityActionType actionType)
    {
        BackgroundJob.Enqueue<IBackgoundService>(x => x.ProcessTrackEngagementMetricJobAsync(trackId, actionType));
        return true;
    }

    public async Task<bool> ProcessTrackDiscoveryAsync(string trackId, PopularityActionType actionType)
    {
        BackgroundJob.Enqueue<IBackgoundService>(x => x.ProcessTrackDiscoveryMetricJobAsync(trackId, actionType));
        return true;
    }

    public async Task<bool> ProcessArtistEngagementAsync(string artistId, PopularityActionType actionType)
    {
        BackgroundJob.Enqueue<IBackgoundService>(x => x.ProcessArtistEngagementMetricJobAsync(artistId, actionType));
        return true;
    }

    public async Task<bool> ProcessArtistDiscoveryAsync(string artistId, PopularityActionType actionType)
    {
        BackgroundJob.Enqueue<IBackgoundService>(x => x.ProcessArtistDiscoveryMetricJobAsync(artistId, actionType));
        return true;
    }
}
