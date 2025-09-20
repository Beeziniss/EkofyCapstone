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

    public async Task<Recording?> GetRecordingAsync(
        [Parent] MonthlyStreamCount monthlyStreamCount,
        DataLoaderCustomOneToOne<Recording> recordingByIdDataLoader,
        CancellationToken cancellationToken)
    {
        if (monthlyStreamCount.RecordingId == null)
        {
            return null;
        }

        return await recordingByIdDataLoader.LoadAsync(monthlyStreamCount.RecordingId, cancellationToken);
    }

    public async Task<Work?> GetWorkAsync(
        [Parent] MonthlyStreamCount monthlyStreamCount,
        DataLoaderCustomOneToOne<Work> workByIdDataLoader,
        CancellationToken cancellationToken)
    {
        if (monthlyStreamCount.WorkId == null)
        {
            return null;
        }

        return await workByIdDataLoader.LoadAsync(monthlyStreamCount.WorkId, cancellationToken);
    }
}
