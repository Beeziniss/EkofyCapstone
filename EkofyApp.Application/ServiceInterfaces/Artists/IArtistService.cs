using EkofyApp.Application.Models.Artists;

namespace EkofyApp.Application.ServiceInterfaces.Artists;
public interface IArtistService
{
    Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest);
}
