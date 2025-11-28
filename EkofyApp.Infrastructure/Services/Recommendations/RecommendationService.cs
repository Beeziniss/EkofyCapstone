using EkofyApp.Application.Models.AudioFeatures;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Recommendations;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Recommendations;
public sealed class RecommendationService(IUnitOfWork unitOfWork) : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Track> GetCamelotRecommendedTracks(AudioFeature audioFeature)
    {
        IEnumerable<(string Key, string Mode)> compatible = CamelotHelper.GetCompatibleKeys(audioFeature.Key, audioFeature.Mode);
        // output:
        // [ ("A", "minor"), ("G", "minor"), ("B", "minor"), ("A", "major") ]

        FilterDefinition<Track> filter = Builders<Track>.Filter.Or(
            compatible.Select(km => Builders<Track>.Filter.And(
                Builders<Track>.Filter.Eq(x => x.AudioFeature.Key, km.Key),
                Builders<Track>.Filter.Eq(x => x.AudioFeature.Mode, km.Mode)
            ))
        );

        return _unitOfWork.GetCollection<Track>().Find(filter).ToEnumerable().AsQueryable();
    }

    public IEnumerable<string> GetCamelotRecommendedTrackIds(AudioFeature audioFeature)
    {
        IEnumerable<(string Key, string Mode)> compatible = CamelotHelper.GetCompatibleKeys(audioFeature.Key, audioFeature.Mode);
        // output:
        // [ ("A", "minor"), ("G", "minor"), ("B", "minor"), ("A", "major") ]

        FilterDefinition<Track> filter = Builders<Track>.Filter.Or(
            compatible.Select(km => Builders<Track>.Filter.And(
                Builders<Track>.Filter.Eq(x => x.AudioFeature.Key, km.Key),
                Builders<Track>.Filter.Eq(x => x.AudioFeature.Mode, km.Mode)
            ))
        );

        return _unitOfWork.GetCollection<Track>().Find(filter).Project(x => x.Id).ToEnumerable();
    }

    public IEnumerable<string> GetCamelotRecommendedTrackIds(IEnumerable<AudioFeature> audioFeatures)
    {
        HashSet<(string Key, string Mode)> allCompatible = [];

        foreach (AudioFeature feature in audioFeatures)
        {
            IEnumerable<(string Key, string Mode)> compatible = CamelotHelper.GetCompatibleKeys(feature.Key, feature.Mode);
            foreach ((string Key, string Mode) item in compatible)
            {
                allCompatible.Add(item); // tránh trùng
            }
        }

        FilterDefinition<Track> filter = Builders<Track>.Filter.Or(
            allCompatible.Select(km =>
                Builders<Track>.Filter.And(
                    Builders<Track>.Filter.Eq(x => x.AudioFeature.Key, km.Key),
                    Builders<Track>.Filter.Eq(x => x.AudioFeature.Mode, km.Mode)
                )
            )
        );

        return _unitOfWork.GetCollection<Track>()
                          .Find(filter)
                          .Project(x => x.Id)
                          .ToEnumerable();
    }

    public IEnumerable<string> GetCamelotRecommendedTrackIds(string trackId)
    {
        // Bước 1: Lấy track theo trackId
        Track track = _unitOfWork.GetCollection<Track>()
                                 .Find(Builders<Track>.Filter.Eq(t => t.Id, trackId))
                                 .FirstOrDefault();

        if (track == null || track.AudioFeature == null)
            return Enumerable.Empty<string>(); // Trả về rỗng nếu không tìm thấy hoặc thiếu AudioFeature

        // Bước 2: Lấy các key + mode tương thích
        IEnumerable<(string Key, string Mode)> compatibleKeys =
            CamelotHelper.GetCompatibleKeys(track.AudioFeature.Key, track.AudioFeature.Mode);

        // Bước 3: Tạo filter để tìm các track tương thích
        FilterDefinition<Track> compatibleFilter = Builders<Track>.Filter.Or(
            compatibleKeys.Select(km =>
                Builders<Track>.Filter.And(
                    Builders<Track>.Filter.Eq(x => x.AudioFeature.Key, km.Key),
                    Builders<Track>.Filter.Eq(x => x.AudioFeature.Mode, km.Mode)
                )
            )
        );

        // (Tuỳ chọn) Loại bỏ track gốc khỏi kết quả
        FilterDefinition<Track> excludeOriginal = Builders<Track>.Filter.Ne(x => x.Id, trackId);
        FilterDefinition<Track> finalFilter = Builders<Track>.Filter.And(compatibleFilter, excludeOriginal);

        // Bước 4: Trả về danh sách ID
        return _unitOfWork.GetCollection<Track>()
                          .Find(finalFilter)
                          .Project(x => x.Id)
                          .ToEnumerable();
    }

    public async Task<IEnumerable<string>> GetCamelotRecommendedTrackIdsAsync(IEnumerable<string> trackIds)
    {
        // Bước 1: Lấy danh sách AudioFeature từ trackIds
        FilterDefinition<Track> inputTracksFilter = Builders<Track>.Filter.In(t => t.Id, trackIds);
        List<AudioFeature> audioFeatures = _unitOfWork.GetCollection<Track>()
                                                      .Find(inputTracksFilter)
                                                      .Project(t => t.AudioFeature)
                                                      .ToList();

        // Bước 2: Lấy các key + mode tương thích
        HashSet<(string Key, string Mode)> allCompatible = [];

        foreach (AudioFeature feature in audioFeatures)
        {
            IEnumerable<(string Key, string Mode)> compatible = CamelotHelper.GetCompatibleKeys(feature.Key, feature.Mode);
            foreach ((string Key, string Mode) item in compatible)
            {
                allCompatible.Add(item); // tránh trùng
            }
        }

        // Bước 3: Tìm các track có key + mode tương thích
        FilterDefinition<Track> compatibleFilter = Builders<Track>.Filter.Or(
            allCompatible.Select(km =>
                Builders<Track>.Filter.And(
                    Builders<Track>.Filter.Eq(x => x.AudioFeature.Key, km.Key),
                    Builders<Track>.Filter.Eq(x => x.AudioFeature.Mode, km.Mode)
                )
            )
        );

        return await _unitOfWork.GetCollection<Track>()
                          .Find(compatibleFilter)
                          .Project(x => x.Id)
                          .Limit(20)
                          .ToListAsync();
    }

    public async Task<Dictionary<string, IEnumerable<string>>> RecommendTracksByTopTracksAsync(IEnumerable<TopTrack> topTracks)
    {
        Dictionary<string, IEnumerable<string>> result = [];

        foreach (TopTrack topTrack in topTracks)
        {
            IEnumerable<string> trackIds = topTrack.TracksInfo.Select(ti => ti.TrackId).ToList();

            if (!trackIds.Any())
            {
                result[topTrack.UserId] = [];
                continue;
            }

            IEnumerable<string> recommendedTrackIds = await GetCamelotRecommendedTrackIdsAsync(trackIds);

            result[topTrack.UserId] = recommendedTrackIds;
        }

        return result;
    }

    public IQueryable<Track> GetEuclideanRecommendedTracks(AudioFeature audioFeature, AudioFeatureWeight weights, int maxResults = 50)
    {
        IEnumerable<string> trackIds = GetEuclideanRecommendedTrackIds(audioFeature, weights, maxResults);
        return GetTracksByIds(trackIds);
    }

    public IQueryable<Track> GetCosineRecommendedTracks(AudioFeature audioFeature, AudioFeatureWeight weights, int maxResults = 50)
    {
        IEnumerable<string> trackIds = GetCosineRecommendedTrackIds(audioFeature, weights, maxResults);
        return GetTracksByIds(trackIds);
    }

    private IEnumerable<string> GetEuclideanRecommendedTrackIds(AudioFeature audioFeature, AudioFeatureWeight weights, int maxResults = 50)
    {
        // Use strongly-typed aggregation pipeline with MongoDB driver
        IEnumerable<Track> tracksData = _unitOfWork.GetCollection<Track>()
            .Aggregate()
            .Project<Track>(Builders<Track>.Projection
                .Include(x => x.Id)
                .Include(x => x.AudioFeature.Tempo)
                .Include(x => x.AudioFeature.Energy)
                .Include(x => x.AudioFeature.Danceability)
                .Include(x => x.AudioFeature.Acousticness))
            .ToEnumerable();

        // Calculate weighted Euclidean distance for each track
        IEnumerable<string> recommendedTrackIds = tracksData
            .Select(track => new
            {
                track.Id,
                Distance = CalculateWeightedEuclideanDistance(audioFeature, track.AudioFeature, weights)
            })
            .Where(x => x.Distance > 0)
            .OrderBy(x => x.Distance)
            .Select(x => x.Id)
            .Take(maxResults)
            .AsEnumerable(); // Lưu thành danh sách để in

        return recommendedTrackIds;
    }

    private IEnumerable<string> GetCosineRecommendedTrackIds(AudioFeature audioFeature, AudioFeatureWeight weights, int maxResults = 50)
    {
        // Use strongly-typed aggregation pipeline with MongoDB driver
        IEnumerable<Track> tracksData = _unitOfWork.GetCollection<Track>()
            .Aggregate()
            .Project<Track>(Builders<Track>.Projection
                .Include(x => x.Id)
                .Include(x => x.AudioFeature.Tempo)
                .Include(x => x.AudioFeature.Energy)
                .Include(x => x.AudioFeature.Danceability)
                .Include(x => x.AudioFeature.Acousticness))
            .ToEnumerable();

        // Calculate weighted cosine similarity for each track
        IEnumerable<string> recommendedTrackIds = tracksData
            .Select(track => new
            {
                track.Id,
                Similarity = CalculateWeightedCosineSimilarity(audioFeature, track.AudioFeature, weights)
            })
            .Where(x => x.Similarity > 0 && x.Similarity < 1)
            .OrderByDescending(x => x.Similarity) // Higher similarity is better
            .Select(x => x.Id)
            .Take(maxResults)
            .AsEnumerable();

        return recommendedTrackIds;
    }

    private IQueryable<Track> GetTracksByIds(IEnumerable<string> trackIds)
    {
        if (trackIds == null || !trackIds.Any())
        {
            return Enumerable.Empty<Track>().AsQueryable();
        }

        FilterDefinition<Track> filter = Builders<Track>.Filter.In(x => x.Id, trackIds);

        return _unitOfWork.GetCollection<Track>().Find(filter).ToEnumerable().AsQueryable();
    }

    private double CalculateWeightedEuclideanDistance(AudioFeature source, AudioFeature target, AudioFeatureWeight weights)
    {
        if (source == null || target == null)
        {
            return double.MaxValue;
        }

        double sum = 0;

        // Only calculate for the remaining features
        sum += CalculateWeightedFeatureDifference((float)Standardize(source.Tempo, 20, 500), (float)Standardize(target.Tempo, 20, 500), weights.Tempo ?? 1.0f);
        sum += CalculateWeightedFeatureDifference(source.Energy, target.Energy, weights.Energy ?? 1.0f);
        sum += CalculateWeightedFeatureDifference(source.Danceability, target.Danceability, weights.Danceability ?? 1.0f);
        sum += CalculateWeightedFeatureDifference(source.Acousticness, target.Acousticness, weights.Acousticness ?? 1.0f);

        return Math.Sqrt(sum);
    }

    private double CalculateWeightedCosineSimilarity(AudioFeature source, AudioFeature target, AudioFeatureWeight weights)
    {
        if (source == null || target == null)
        {
            return 0.0;
        }

        double tempoStandardization1 = Standardize(source.Tempo, 20, 500);
        double tempoStandardization2 = Standardize(target.Tempo, 20, 500);

        // Create weighted feature vectors for the remaining features only
        double[] sourceVector = [
            tempoStandardization1 * (weights.Tempo ?? 1.0f),
            source.Energy * (weights.Energy ?? 1.0f),
            source.Danceability * (weights.Danceability ?? 1.0f),
            source.Acousticness * (weights.Acousticness ?? 1.0f)
        ];

        double[] targetVector = [
            tempoStandardization2 * (weights.Tempo ?? 1.0f),
            target.Energy * (weights.Energy ?? 1.0f),
            target.Danceability * (weights.Danceability ?? 1.0f),
            target.Acousticness * (weights.Acousticness ?? 1.0f)
        ];

        return CalculateCosineSimilarity(sourceVector, targetVector);
    }

    private double CalculateCosineSimilarity(double[] vectorA, double[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        //double dotProduct = 0.0;
        //double magnitudeA = 0.0;
        //double magnitudeB = 0.0;

        //for (int i = 0; i < vectorA.Length; i++)
        //{
        //    dotProduct += vectorA[i] * vectorB[i];
        //    magnitudeA += vectorA[i] * vectorA[i];
        //    magnitudeB += vectorB[i] * vectorB[i];
        //}

        //magnitudeA = Math.Sqrt(magnitudeA);
        //magnitudeB = Math.Sqrt(magnitudeB);

        //if (magnitudeA == 0.0 || magnitudeB == 0.0)
        //{
        //    return 0.0; // Avoid division by zero
        //}

        //return dotProduct / (magnitudeA * magnitudeB);

        double dotProduct = vectorA.Zip(vectorB, (a, b) => a * b).Sum();
        double magnitude1 = vectorA.Sum(a => a * a);
        double magnitude2 = vectorB.Sum(b => b * b);

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0.0;
        }

        double cosineSimilarity = dotProduct / Math.Sqrt(magnitude1 * magnitude2);

        return cosineSimilarity;
    }

    private double CalculateWeightedFeatureDifference(float sourceValue, float targetValue, float weight)
    {
        double difference = sourceValue - targetValue;
        return weight * difference * difference;
    }

    private double Standardize(double value, double min = 0, double max = 0)
    {
        bool isValidMinMax = min < max;
        bool isValidMinValue = value >= min;
        bool isValidMaxValue = value <= max;
        bool isValidValue = isValidMinMax && isValidMinValue && isValidMaxValue;

        if (!isValidValue)
        {
            throw new BadRequestCustomException("Invalid value");
        }

        return (value - min) / (max - min);
    }
}
