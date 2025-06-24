using EkofyApp.Application.Models.Track;

namespace EkofyApp.Application.ServiceInterfaces.Track;
public interface ITrackGraphQLService
{
    IQueryable<TrackResponse> GetTracksExecutable();
}
