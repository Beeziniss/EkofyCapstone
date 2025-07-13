using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Artists;
public sealed class ArtistGraphQLService(IUnitOfWork unitOfWork) : IArtistGraphQLService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Artist> GetArtistsQueryable()
    {
        // Trả về IQueryable của Artist từ UnitOfWork
        return _unitOfWork.GetCollection<Artist>().AsQueryable();
    }
}
