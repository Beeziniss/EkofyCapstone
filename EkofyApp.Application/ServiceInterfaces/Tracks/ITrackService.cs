using EkofyApp.Application.Models.Tracks;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Tracks
{
    public interface ITrackService
    {
        Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse);
        TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest);
        Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id);
        IQueryable<Track> GetTracksQueryable();
    }
}
