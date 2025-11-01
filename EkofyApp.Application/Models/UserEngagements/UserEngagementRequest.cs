using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.UserEngagements;

public sealed record UserEngagementRequest
{
    public string TargetId { get; init; } = null!;
}