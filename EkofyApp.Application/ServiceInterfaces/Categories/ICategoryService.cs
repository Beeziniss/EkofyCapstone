using EkofyApp.Application.Models.Categories;
using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Application.ServiceInterfaces.Categories;
public interface ICategoryService
{
    Task CreateCategoryAsync(CreateCategoryRequest createCategoryRequest);
    Task<IEnumerable<string>> GetMoodsFromAudioFeaturesAsync(AudioFeature audioFeature);
}
