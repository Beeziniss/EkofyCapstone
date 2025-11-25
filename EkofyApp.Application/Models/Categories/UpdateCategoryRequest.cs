using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Categories;

public sealed record class UpdateCategoryRequest
{
    public string CategoryId { get; init; } = null!; // ID of the category to be updated
    public string? Name { get; init; } // Name of the category, e.g., "Rock", "Jazz"
    public string? Description { get; init; } // Description of the category
    public CategoryType? Type { get; init; } // Type of the category (Genre or Mood)
    public List<string>? Aliases { get; init; } // Alternative names for SEO
    public int? Popularity { get; init; } // Popularity score
    public bool? IsVisible { get; init; } // Visibility status
}