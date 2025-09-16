using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class EntitlementRoleDefault
{
    public UserRole Role { get; set; }
    public object Value { get; set; } = null!;
}
