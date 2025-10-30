using EkofyApp.Domain.Entities;

namespace EkofyApp.Domain.Utils;
public static class InMemoryPolicyCache
{
    public static RoyaltyPolicy RoyaltyPolicy { get; set; } = null!;
    public static LegalPolicy LegalPolicy { get; set; } = null!;
}
