using EkofyApp.Application.Models.TopTracks;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.TopTracks
{
    public interface ITopTrackService
    {
        IQueryable<TopTrack> GetOwnTopTracks();
        Task<IEnumerable<TopTrack>> GetTopTrackBysUserIds(IEnumerable<string> userIds);
        IQueryable<TopTrackResponse> GetTopTracksByUserId(string userId);
        Task UpsertTopTrackCountAsync(string trackId, string userId, CancellationToken cancellationToken = default);
    }
}
