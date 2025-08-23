using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class IdentityCard
{
    public string Number { get; set; } = null!; // Số căn cước công dân
    public string FullName { get; set; } = null!;
    public DateTimeOffset DateOfBirth { get; set; }
    public UserGender Gender { get; set; }
    public string PlaceOfOrigin { get; set; } = null!;
    public string Nationality { get; set; } = null!;

    public Address PlaceOfResidence { get; set; } = null!; // Nested object
    public string? FrontImage { get; set; } // Ảnh mặt trước CCCD
    public string? BackImage { get; set; }  // Ảnh mặt sau CCCD

    public DateTimeOffset? ValidUntil { get; set; } // Ngày hết hạn của CCCD, nếu không có thì để null
}
