using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.Models.Tracks;
using EkofyApp.Application.Models.Uploads;
using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Tracks;

public interface ITrackService
{
    Task AddEmbeddingVectorAsync();
    Task CreateTrackFromTrackUploadRequestAsync(TrackTempResponse trackResponse, WorkTempRequest workTempRequest, RecordingTempRequest recordingTempRequest);
    TrackTempRequest CreateTrackTemp(CreateTrackRequest createTrackRequest);
    Task<float[]> GenerateEmbeddingsAsync(string term);
    Task<IEnumerable<Track>> GetAllTracksBySemanticAsync(string text, int limit = 20);
    Task<PaginatedData<CombinedUploadRequest>> GetPendingTrackUploadRequestsAsync(int pageNumber = 1, int pageSize = 20);
    Task<CombinedUploadRequest> GetPendingTrackUploadRequestByIdAsync(string uploadId);
    Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id);
    IQueryable<Track> GetTracks();
    Task<bool> CheckTrackInFavoriteAsync(string trackId);
    Task ReleaseScheduledTrackAsync(string trackId);
    IQueryable<Track> SearchTracks(string searchTerm);
    Task<long> AddToFavoriteTrackAsync(string trackId, bool isAdding);
    Task UpdateStreamCount(string trackId);
    IQueryable<Track> GetFavoriteTracks();
    Task SeedMonthlyStreamCountByTrackIdAsync(string trackId, long streamCount, int month, int year);
    IQueryable<Track> GetEuclideanRecommendedTracksByTrackId(string trackId, AudioFeatureWeight audioFeatureWeight, int limit = 10);
    IQueryable<Track> GetCosineRecommendedTracksByTrackId(string trackId, AudioFeatureWeight audioFeatureWeight, int limit = 10);
    Task ApproveAutomaticallyAsync(string userId, byte[] bytes, CreateTrackRequest createTrackRequest, CreateWorkRequest createWorkRequest, CreateRecordingRequest createRecordingRequest);
    Task<bool> ApproveTrackUploadRequestAsync(string actionByUserId, string uploadId);
}
