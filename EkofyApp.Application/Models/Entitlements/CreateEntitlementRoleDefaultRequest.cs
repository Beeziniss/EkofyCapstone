using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Entitlements;
public sealed record class CreateEntitlementRoleDefaultRequest
{
    public UserRole Role { get; init; }
    public object Value { get; init; } = null!;
}
