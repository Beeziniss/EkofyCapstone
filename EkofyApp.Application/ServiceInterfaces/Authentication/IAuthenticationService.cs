using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Listeners;

namespace EkofyApp.Application.ServiceInterfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthListenerTokenResponse> LoginListenerAsync(LoginRequest loginRequest);
        Task RegisterListenerAsync(ListenerRegisterRequest registerRequest);
    }
}
