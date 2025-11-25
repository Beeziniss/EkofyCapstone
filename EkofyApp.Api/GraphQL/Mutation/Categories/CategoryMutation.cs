using EkofyApp.Application.Models.Categories;
using EkofyApp.Application.ServiceInterfaces.Categories;

namespace EkofyApp.Api.GraphQL.Mutation.Categories;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public class CategoryMutation(ICategoryService categoryService)
{
    private readonly ICategoryService _categoryService = categoryService;

    public async Task<bool> CreateCategoryAsync(CreateCategoryRequest categoryRequest)
    {
        await _categoryService.CreateCategoryAsync(categoryRequest);
        return true;
    }

    public async Task<bool> UpdateCategoryAsync(UpdateCategoryRequest updateCategoryRequest)
    {
        await _categoryService.UpdateCategoryAsync(updateCategoryRequest);
        return true;
    }

    public async Task<bool> SoftDeleteCategoryAsync(string categoryId)
    {
        await _categoryService.SoftDeleteCategoryAsync(categoryId);
        return true;
    }

    public async Task<bool> HardDeleteCategoryAsync(DeleteCategoryRequest deleteCategoryRequest)
    {
        await _categoryService.HardDeleteCategoryAsync(deleteCategoryRequest);
        return true;
    }
}
