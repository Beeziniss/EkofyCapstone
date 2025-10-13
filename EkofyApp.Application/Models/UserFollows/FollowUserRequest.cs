namespace EkofyApp.Application.Models.UserFollows;

public sealed record FollowUserRequest
{
    public string TargetUserId { get; init; } = null!;
}