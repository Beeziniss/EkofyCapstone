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

    public async Task<User> GetUserByIdAsync(string id)
    {
        ProjectionDefinition<User> projection = Builders<User>.Projection
            .Exclude(x => x.FCMToken)
            .Exclude(x => x.PasswordHash)
            .Exclude(x => x.RefreshToken)
            .Exclude(x => x.RefreshTokenExpiryTime);

        return await _unitOfWork.GetCollection<User>()
            .Find(x => x.Id == id)
            .Project<User>(projection)
            .FirstOrDefaultAsync();
    }
}
