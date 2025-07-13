using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Categories;
public sealed record class CreateCategoryRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
    public CategoryType Type { get; init; }
}
