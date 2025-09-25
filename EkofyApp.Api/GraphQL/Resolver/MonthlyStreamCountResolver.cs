using EkofyApp.Api.GraphQL.DataLoader;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Resolver;

[ExtendObjectType(typeof(MonthlyStreamCount))]
public sealed class MonthlyStreamCountResolver
{
    public async Task<Track?> GetTrackAsync(
        [Parent] MonthlyStreamCount monthlyStreamCount,
        DataLoaderCustomOneToOne<Track> trackByIdDataLoader,
        CancellationToken cancellationToken)
    {
        return await trackByIdDataLoader.LoadAsync(monthlyStreamCount.TrackId, cancellationToken);
    }
}
