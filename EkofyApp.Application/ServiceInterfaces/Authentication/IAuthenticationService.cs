using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Admins;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.Models.Auth.Moderators;

namespace EkofyApp.Application.ServiceInterfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthAdminTokenResponse> LoginAdminAsync(LoginRequest loginRequest);
        Task<AuthArtistTokenResponse> LoginArtistAsync(LoginRequest loginRequest);
        Task<AuthListenerTokenResponse> LoginListenerAsync(LoginRequest loginRequest);
        Task<AuthModeratorTokenResponse> LoginModeratorAsync(LoginRequest loginRequest);
        Task RegisterArtistAsync(ArtistRegisterRequest registerRequest);
        Task RegisterListenerAsync(ListenerRegisterRequest registerRequest);
    }
}
