using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Domain.Entities;
using HealthyNutritionApp.Application.Interfaces;

namespace EkofyApp.Infrastructure.Services.Artists
{
    public class ArtistService(IUnitOfWork unitOfWork) : IArtistService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest)
        {
            Artist artist = new()
            {
                Name = createArtistRequest.Name,
                Genre = createArtistRequest.Genre,
            };

            await _unitOfWork.GetCollection<Artist>().InsertOneAsync(artist);

            return true;
        }
    }
}
