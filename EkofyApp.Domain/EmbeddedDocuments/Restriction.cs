using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Restriction
{
    public RestrictionType Type { get; set; } = RestrictionType.None;
    public string? Reason { get; set; }
    public DateTime? RestrictedAt { get; set; }
    public DateTime? Expired { get; set; }

    // TODO: Chưa rõ cách sử dụng
    //public abstract bool IsActive(DateTime now);
}
