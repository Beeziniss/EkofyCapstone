using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Recommendations;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Recommendations;
public sealed class RecommendationService(IUnitOfWork unitOfWork) : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Track> GetCamelotRecommendedTracks(AudioFeature audioFeature)
    {
        List<(string Key, string Mode)> compatible = CamelotHelper.GetCompatibleKeys(audioFeature.Key, audioFeature.Mode);
        // output:
        // [ ("A", "minor"), ("G", "minor"), ("B", "minor"), ("A", "major") ]

        FilterDefinition<Track> filter = Builders<Track>.Filter.Or(
            compatible
            .Select(km => Builders<Track>.Filter.And(
                Builders<Track>.Filter.Eq(x => x.AudioFeature.Key, km.Key),
                Builders<Track>.Filter.Eq(x => x.AudioFeature.Mode, km.Mode)
            ))
        );

        return _unitOfWork.GetCollection<Track>().Find(filter).ToEnumerable().AsQueryable();
    }
}
