using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Categories;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class CategoryQuery(ICategoryService categoryService)
{
    private readonly ICategoryService _categoryService = categoryService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Category>]
    public IQueryable<Category> GetCategories()
    {
        return _categoryService.GetCategories();
    }
}
