using EkofyApp.Application.Models.Auth;

namespace EkofyApp.Application.ServiceInterfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<string> LoginAsync(string email, string password);
        Task RegisterListenerAsync(ListenerRegisterRequest registerRequest);
    }
}
