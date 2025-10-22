using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Tracks
{
    public interface ITrackService
    {
        Task AddEmbeddingVectorAsync();
        Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse, WorkTempRequest workTempRequest, RecordingTempRequest recordingTempRequest);
        TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest);
        Task<float[]> GenerateEmbeddingsAsync(string term);
        Task<IEnumerable<Track>> GetAllTracksBySemanticAsync(string text, int limit = 20);
        Task<PaginatedData<CombinedUploadRequest>> GetPendingTrackUploadRequestsAsync(int pageNumber = 1, int pageSize = 20);
        Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id);
        IQueryable<Track> GetTracks();
        Task ReleaseScheduledTrackAsync(string trackId);
        IQueryable<Track> SearchTracks(string searchTerm);
        Task<long> UpdateFavoriteCountAsync(string trackId, long incrementValue);
        Task UpdateStreamCount(string trackId);
    }
}
