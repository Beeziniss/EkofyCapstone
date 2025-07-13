using EkofyApp.Application.Models.Categories;

namespace EkofyApp.Application.ServiceInterfaces.Categories;
public interface ICategoryService
{
    Task CreateCategoryAsync(CreateCategoryRequest createCategoryRequest);
}
