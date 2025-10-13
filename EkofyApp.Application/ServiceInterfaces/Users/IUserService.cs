using EkofyApp.Application.Models.UserFollows;
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
    Task ReActiveUserAsync(string targetUserId);
    IQueryable<Follows> GetUserFollows();
    Task FollowUserAsync(FollowUserRequest request);
    Task UnfollowUserAsync(UnfollowUserRequest request);
}
