using EkofyApp.Application.Models.UserEngagements;
using EkofyApp.Application.Models.Users;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Users;
public interface IUserService
{
    Task CreateAdminAsync(CreateAdminRequest createAdminRequest);
    Task CreateModeratorAsync(CreateModeratorRequest createModeratorRequest);
    Task BanUserAsync(string targetUserId);
    Task<User> GetUserByIdAsync(string id);
    IQueryable<User> GetUsers();
    Task UnbanUserAsync(string targetUserId);
    IQueryable<UserEngagement> GetUserEngagement();
    Task FollowUserAsync(UserEngagementRequest request);
    Task UnfollowUserAsync(UserEngagementRequest request);
    Task DeleteUserManualAsync(string userId);
    Task<bool> CheckUserFollowingAsync(string userFollowingId);
    IQueryable<User> GetFollowersByUserId(string userId);
    IQueryable<User> GetFollowingsByUserId(string userId);
}
