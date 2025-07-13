using EkofyApp.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace EkofyApp.Domain.Entities;
public sealed class Category : IEntityCustom
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; }

    public string Name { get; set; }
    public string Slug { get; set; }
    public CategoryType Type { get; set; } // e.g., "music", "podcast", etc.
    public List<string> Aliases { get; set; } = []; // For SEO or alternative names

    public int Popularity { get; set; } // A measure of how popular the category is

    public string Description { get; set; }
    public bool IsVisible { get; set; } = true; // Indicates if the category is visible to users
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
