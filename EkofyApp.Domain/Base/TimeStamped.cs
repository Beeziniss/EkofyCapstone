using EkofyApp.Domain.Utils;

namespace EkofyApp.Domain.Base;
public abstract class TimeStamped
{
    public DateTimeOffset CreatedAt { get; set; } = HelperMethod.GetUtcPlus7TimeOffset();
    public DateTimeOffset? UpdatedAt { get; set; }
}

