namespace EkofyApp.Application.ServiceInterfaces.Authentication
{
    public interface IAuthenticationService
    {
        Task<string> LoginAsync(string email, string password);
    }
}
