using EkofyApp.Application.Models.Categories;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.ServiceInterfaces.Categories;
public interface ICategoryService
{
    Task CreateCategoryAsync(CreateCategoryRequest createCategoryRequest);
    Task UpdateCategoryAsync(UpdateCategoryRequest updateCategoryRequest);
    Task SoftDeleteCategoryAsync(string categoryId);
    Task HardDeleteCategoryAsync(DeleteCategoryRequest deleteCategoryRequest);
    IEnumerable<MoodType> DetectMoods(AudioFeature feature);
    string GenerateAlternativeDescription(AudioFeature audioFeature, IEnumerable<MoodType> moods);
    IQueryable<Category> GetCategories();
    Task<IEnumerable<string>> GetMoodsFromAudioFeaturesAsync(IEnumerable<MoodType> moodTypes);
}
