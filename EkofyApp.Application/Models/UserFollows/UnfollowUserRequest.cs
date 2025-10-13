namespace EkofyApp.Application.Models.UserFollows;

public sealed record UnfollowUserRequest
{
    public string TargetUserId { get; init; } = null!;
}