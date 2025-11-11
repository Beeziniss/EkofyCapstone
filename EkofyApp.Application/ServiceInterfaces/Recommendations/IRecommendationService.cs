using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Recommendations;
public interface IRecommendationService
{
    IQueryable<Track> GetCamelotRecommendedTracks(AudioFeature audioFeature);
    IQueryable<Track> GetEuclideanRecommendedTracks(AudioFeature audioFeature, AudioFeatureWeight weights, int maxResults = 50);
    IQueryable<Track> GetCosineRecommendedTracks(AudioFeature audioFeature, AudioFeatureWeight weights, int maxResults = 50);
}
