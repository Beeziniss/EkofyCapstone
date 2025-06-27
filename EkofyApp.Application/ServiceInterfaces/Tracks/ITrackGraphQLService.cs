using EkofyApp.Application.Models.Tracks;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;
public interface ITrackGraphQLService
{
    IQueryable<TrackResponse> GetTracksExecutable();
}
