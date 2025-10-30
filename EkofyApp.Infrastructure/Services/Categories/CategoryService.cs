using EkofyApp.Application.Models.Categories;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Categories;
public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Category> GetCategories()
    {
        return _unitOfWork.GetCollection<Category>().AsQueryable();
    }

    public async Task CreateCategoryAsync(CreateCategoryRequest createCategoryRequest)
    {
        Category category = new()
        {
            Name = createCategoryRequest.Name,
            Description = createCategoryRequest.Description,
            Type = createCategoryRequest.Type,
            //Slug = HelperMethod.GenerateSlug(createCategoryRequest.DisplayName),
            Slug = createCategoryRequest.Name?.ToLowerInvariant().Replace(" ", "-") ?? string.Empty,
            Popularity = 0,
            CreatedAt = HelperMethod.GetUtcPlus7TimeOffset(),
        };

        await _unitOfWork.GetCollection<Category>().InsertOneAsync(category);
    }

    public async Task<IEnumerable<string>> GetMoodsFromAudioFeaturesAsync(IEnumerable<MoodType> moodTypes)
    {
        // Xác định mood của track dựa trên đặc trưng âm thanh
        if (moodTypes.Any())
        {
            // Convert moodTypes to string (to compare with mood.DisplayName)
            IEnumerable<string> moodTypeNames = moodTypes.Select(mt => mt.ToString()).ToList();

            return await _unitOfWork.GetCollection<Category>()
                .Find(mood => mood.Type == CategoryType.Mood && moodTypeNames.Contains(mood.Name))
                .Project(mood => mood.Id)
                .ToListAsync();
        }

        return [];
    }

    #region Mood Detection
    public IEnumerable<MoodType> DetectMoods(AudioFeature feature)
    {
        List<MoodType> moods = [];

        // Happy
        if (feature.Tempo > 110 &&
            feature.ModeNumber == 1 &&
            feature.Energy > 0.5 &&
            feature.Danceability > 0.5)
        {
            moods.Add(MoodType.Happy);
        }

        // Calm
        if (feature.Tempo < 90 &&
            feature.Acousticness > 0.5 &&
            feature.Energy < 0.3 &&
            feature.ZeroCrossingRate < 0.05)
        {
            moods.Add(MoodType.Calm);
        }

        // Sad
        if (feature.Tempo < 85 &&
            feature.ModeNumber == 0 &&
            feature.Energy < 0.3 &&
            feature.SpectralCentroid < 2500 &&
            IsMfccLow(feature.MfccMean))
        {
            moods.Add(MoodType.Sad);
        }

        // Angry
        if (feature.Energy > 0.7 &&
            feature.ZeroCrossingRate > 0.1 &&
            feature.SpectralCentroid > 4500 &&
            feature.ModeNumber == 0 &&
            IsMfccUnstable(feature.MfccMean))
        {
            moods.Add(MoodType.Angry);
        }

        // Relaxed
        if (feature.Tempo < 100 &&
            feature.Danceability > 0.5 &&
            feature.Acousticness > 0.4)
        {
            moods.Add(MoodType.Relaxed);
        }

        // Energetic
        if (feature.Tempo > 120 &&
            feature.Energy > 0.6 &&
            feature.Danceability > 0.6)
        {
            moods.Add(MoodType.Energetic);
        }

        // Dark
        if (feature.SpectralCentroid < 2000 &&
            feature.Energy < 0.4 &&
            feature.ModeNumber == 0)
        {
            moods.Add(MoodType.Dark);
        }

        // Romantic
        if (feature.Tempo >= 60 && feature.Tempo <= 90 &&
            feature.ChromaMean.Average() > 0.5 &&
            feature.Energy < 0.5)
        {
            moods.Add(MoodType.Romantic);
        }

        // Chill
        if (feature.Energy < 0.4 &&
            feature.Acousticness > 0.4 &&
            feature.Tempo < 100)
        {
            moods.Add(MoodType.Chill);
        }

        return moods.Distinct().ToList();
    }

    private bool IsMfccLow(List<float> mfcc)
    {
        return mfcc.Count > 0 && mfcc.Average() < 0;
    }

    private bool IsMfccUnstable(List<float> mfcc)
    {
        if (mfcc.Count < 2)
        {
            return false;
        }

        double stdDev = Math.Sqrt(mfcc.Average(x => Math.Pow(x - mfcc.Average(), 2)));
        return stdDev > 30;
    }
    #endregion

    public string GenerateAlternativeDescription(AudioFeature audioFeature, IEnumerable<MoodType> moods)
    {
        string tempoDesc = audioFeature.Tempo switch
        {
            < 80 => "rất chậm và nhẹ nhàng",
            >= 80 and < 100 => "chậm và êm dịu",
            >= 100 and < 120 => "vừa phải, dễ theo nhịp",
            >= 120 and < 140 => "nhanh và sôi động",
            _ => "rất nhanh, mang năng lượng cao"
        };

        string energyDesc = audioFeature.Energy switch
        {
            < 0.3F => "êm dịu và ít năng lượng",
            >= 0.3F and < 0.6F => "có mức năng lượng vừa phải",
            _ => "mạnh mẽ và tràn đầy năng lượng"
        };

        string danceabilityDesc = audioFeature.Danceability switch
        {
            < 0.3F => "khó nhảy hoặc thiên về nghe hơn",
            >= 0.3F and < 0.6F => "có thể nhảy nhẹ hoặc đung đưa",
            _ => "rất dễ nhảy và bắt tai"
        };

        string acousticnessDesc = audioFeature.Acousticness switch
        {
            < 0.3F => "chủ yếu sử dụng nhạc cụ điện tử",
            >= 0.3F and < 0.6F => "kết hợp giữa điện tử và mộc",
            _ => "mang âm hưởng acoustic tự nhiên"
        };

        string modeDesc = audioFeature.ModeNumber == 1 ? "ở tông trưởng (vui tươi, sáng sủa)" : "ở tông thứ (buồn hoặc sâu lắng)";

        string spectralDesc = audioFeature.SpectralCentroid switch
        {
            < 2000 => "có âm sắc tối và ấm áp",
            >= 2000 and < 4000 => "có âm sắc cân bằng",
            _ => "có âm sắc sáng và gắt"
        };

        string mfccDesc = IsMfccLow(audioFeature.MfccMean)
            ? "giai điệu mềm mại, ổn định"
            : IsMfccUnstable(audioFeature.MfccMean)
                ? "âm thanh biến thiên, hơi gắt"
                : "giai điệu trung tính, dễ nghe";

        string moodDesc = "Chung";
        if (moods != null && moods.Any())
        {
            moodDesc = TranslateMoods(moods);
        }

        return $"Bài hát này có nhịp độ {tempoDesc}, {energyDesc}, {danceabilityDesc}, " +
               $"{acousticnessDesc}, {modeDesc}, {spectralDesc}, và {mfccDesc}. " +
               $"Tổng thể, bài hát gợi lên tâm trạng: {moodDesc}.";
    }

    private string TranslateMoods(IEnumerable<MoodType> moods)
    {
        return !moods.Any()
            ? "Không xác định"
            : string.Join(", ", moods.Select(mood => mood switch
        {
            MoodType.Happy => "Vui tươi, Hạnh phúc",
            MoodType.Calm => "Bình yên, Giản dị",
            MoodType.Sad => "Buồn bã",
            MoodType.Angry => "Giận dữ, Tức giận",
            MoodType.Relaxed => "Thư giãn",
            MoodType.Energetic => "Tràn đầy năng lượng",
            MoodType.Dark => "Tối tăm, U ám",
            MoodType.Romantic => "Lãng mạn",
            MoodType.Chill => "Chill, Thoải mái",
            _ => "Không xác định"
        }));
    }
}
