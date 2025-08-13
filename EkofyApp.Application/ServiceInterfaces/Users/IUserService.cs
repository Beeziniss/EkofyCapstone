using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Users;
public interface IUserService
{
    IQueryable<User> GetUsersQueryable();
}
