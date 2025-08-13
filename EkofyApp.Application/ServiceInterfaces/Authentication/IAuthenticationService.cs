using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;

namespace EkofyApp.Application.ServiceInterfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthArtistTokenResponse> LoginArtistAsync(LoginRequest loginRequest);
        Task<AuthListenerTokenResponse> LoginListenerAsync(LoginRequest loginRequest);
        Task RegisterArtistAsync(ArtistRegisterRequest registerRequest);
        Task RegisterListenerAsync(ListenerRegisterRequest registerRequest);
    }
}
