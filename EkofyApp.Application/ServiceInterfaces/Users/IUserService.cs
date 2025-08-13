using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Users;
public interface IUserService
{
    Task<User> GetUserByIdAsync(string id);
    IQueryable<User> GetUsersQueryable();
}
