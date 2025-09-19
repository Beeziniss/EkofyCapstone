using EkofyApp.Application.Models.Categories;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Categories;
public interface ICategoryService
{
    Task CreateCategoryAsync(CreateCategoryRequest createCategoryRequest);
    IQueryable<Category> GetCategories();
    Task<IEnumerable<string>> GetMoodsFromAudioFeaturesAsync(AudioFeature audioFeature);
}
