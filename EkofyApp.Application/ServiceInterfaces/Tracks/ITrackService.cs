using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Works;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Tracks
{
    public interface ITrackService
    {
        Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse, WorkTempRequest workTempRequest, RecordingTempRequest recordingTempRequest);
        TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest);
        Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id);
        IQueryable<Track> GetTracksQueryable();
        Task UpdateStreamCount(string trackId);
    }
}
