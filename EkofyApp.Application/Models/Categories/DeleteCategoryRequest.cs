namespace EkofyApp.Application.Models.Categories;

public sealed record class DeleteCategoryRequest
{
    public string CategoryId { get; init; } = null!; // ID of the category to be deleted
}