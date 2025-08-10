using EkofyApp.Domain.Utils;

namespace EkofyApp.Domain.Base;
public abstract class TimeStamped
{
    public DateTime CreatedAt { get; set; } = HelperMethod.GetUtcPlus7Time();
    public DateTime? UpdatedAt { get; set; }
}

