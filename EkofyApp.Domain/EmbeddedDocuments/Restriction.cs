using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Restriction // TODO: Chưa rõ cách sử dụng vì có status trong User rồi nên cái này có thể thay thế cho report "stuff"
{
    public RestrictionType Type { get; set; } = RestrictionType.None;
    public string? Reason { get; set; }
    public DateTimeOffset? RestrictedAt { get; set; }
    public DateTimeOffset? Expired { get; set; }

    // TODO: Chưa rõ cách sử dụng
    //public abstract bool IsActive(DateTime now);
}
