using EkofyApp.Application.Models.Categories;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Categories;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Infrastructure.Services.Categories;
public class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task CreateCategoryAsync(CreateCategoryRequest createCategoryRequest)
    {
        Category category = new()
        {
            Name = createCategoryRequest.Name,
            Description = createCategoryRequest.Description,
            Type = createCategoryRequest.Type,
            //Slug = HelperMethod.GenerateSlug(createCategoryRequest.Name),
            Slug = createCategoryRequest.Name?.ToLowerInvariant().Replace(" ", "-") ?? string.Empty,
            Popularity = 0,
            CreatedAt = HelperMethod.GetUtcPlus7Time(),
        };

        await _unitOfWork.GetCollection<Category>().InsertOneAsync(category);
    }
}
