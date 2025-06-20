using EkofyApp.Application.Models.Tracks;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Application.ServiceInterfaces.Track
{
    public interface ITrackService
    {
        Task CreateTrackAsync(TrackRequest trackResponse);
        Task<TrackResponse> GetTrackResolverContext(ProjectionDefinition<Tracks> projection, string id);
        Task<IEnumerable<TrackResponse>> GetTracksAsync();
    }
}
