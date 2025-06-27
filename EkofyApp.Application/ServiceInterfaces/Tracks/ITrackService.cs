using EkofyApp.Application.Models.Tracks;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Tracks
{
    public interface ITrackService
    {
        Task CreateTrackAsync(CreateTrackRequest trackResponse);
        Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Track> projection, string id);
        Task<IEnumerable<TrackResponse>> GetTracksAsync();
    }
}
