using EkofyApp.Application.Models.TopTracks;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.TopTracks
{
    public interface ITopTrackService
    {
        IQueryable<TopTrackResponse> GetTopTracksByUserId();
        Task UpsertTopTrackCountAsync(string trackId, CancellationToken cancellationToken = default);
    }
}
