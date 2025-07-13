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
}
