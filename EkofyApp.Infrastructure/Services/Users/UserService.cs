using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Users;
public sealed class UserService(IUnitOfWork unitOfWork) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<User> GetUsersQueryable()
    {
        return _unitOfWork.GetCollection<User>().AsQueryable();
    }
}
