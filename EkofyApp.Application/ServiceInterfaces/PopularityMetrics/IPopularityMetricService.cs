using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.ServiceInterfaces.PopularityMetrics;

public interface IPopularityMetricService
{
    Task ProcessArtistDiscoveryMetricAsync(string artistId, PopularityActionType actionType);
    Task ProcessArtistEngagementMetricAsync(string artistId, PopularityActionType actionType);
    Task ProcessTrackDiscoveryMetricAsync(string trackId, PopularityActionType actionType);
    Task ProcessTrackEngagementMetricAsync(string trackId, PopularityActionType actionType);
    Task ProcessTrackStreamingMetricAsync(string trackId, PopularityActionType actionType);
}
