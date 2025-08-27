using EkofyApp.Application.Models.Users;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Users;
public interface IUserService
{
    Task CreateAdminAsync(CreateAdminRequest createAdminRequest);
    Task CreateModeratorAsync(CreateModeratorRequest createModeratorRequest);
    Task<User> GetUserByIdAsync(string id);
    IQueryable<User> GetUsersQueryable();
}
