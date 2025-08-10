using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class IdentityCard
{
    public string Number { get; set; } = null!; // Số căn cước công dân
    public string FullName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public UserGender Gender { get; set; }
    public string PlaceOfOrigin { get; set; } = null!;
    public string Nationality { get; set; } = null!;

    public Address PlaceOfResidence { get; set; } = null!; // Nested object
    public string? FrontImageUrl { get; set; } // Ảnh mặt trước CCCD
    public string? BackImageUrl { get; set; }  // Ảnh mặt sau CCCD
}
