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

    public async Task<IEnumerable<string>> GetMoodsFromAudioFeaturesAsync(AudioFeature audioFeature)
    {
        // Xác định mood của track dựa trên đặc trưng âm thanh
        IEnumerable<MoodType> moodTypes = HelperMethod.DetectMoods(audioFeature);

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
}
