using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(RoyaltyReport))]
public sealed class RoyaltyReportResolver
{
    public async Task<Track?> GetTrackAsync(
        [Parent] RoyaltyReport royaltyReport,
        DataLoaderCustomOneToOne<Track> trackByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await trackByIdDataLoader.LoadAsync(royaltyReport.TrackId, cancellationToken);
    }

    public async Task<IEnumerable<User?>> GetUsersAsync(
        [Parent] RoyaltyReport royaltyReport,
        DataLoaderCustomOneToOne<User> userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        List<string> userIds = royaltyReport.RoyaltySplits.Select(rs => rs.UserId).ToList();
        return await userByIdDataLoader.LoadAsync(userIds, cancellationToken) ?? [];
    }
}
