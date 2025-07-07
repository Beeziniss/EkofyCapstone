using EkofyApp.Application.Models.Tracks;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;
public interface ITrackGraphQLService
{
    IQueryable<TrackResponse> GetTracksExecutable();
    IQueryable<Track> GetTracksQueryable();
}
