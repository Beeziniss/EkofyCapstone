using EkofyApp.Application.Models.Artist;

namespace EkofyApp.Application.ServiceInterfaces.Artist;
public interface IArtistService
{
    Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest);
}
