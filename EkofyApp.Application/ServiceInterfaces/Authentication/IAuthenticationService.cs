using EkofyApp.Application.Models.Auth;
using EkofyApp.Application.Models.Auth.Admins;
using EkofyApp.Application.Models.Auth.Artists;
using EkofyApp.Application.Models.Auth.Listeners;
using EkofyApp.Application.Models.Auth.Moderators;
using EkofyApp.Application.Models.Users;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace EkofyApp.Application.ServiceInterfaces.Authentication;
public interface IAuthenticationService
{
    Task<CurrentUserProfile> GetCurrentUserProfileAsync();
    Task<AuthAdminTokenResponse> LoginAdminAsync(LoginRequest loginRequest);
    Task<AuthArtistTokenResponse> LoginArtistAsync(LoginRequest loginRequest);
    Task<AuthListenerTokenResponse> LoginListenerAsync(LoginRequest loginRequest);
    Task<AuthModeratorTokenResponse> LoginModeratorAsync(LoginRequest loginRequest);
    Task LogoutAsync(bool isMobile = false);
    Task<AccessTokenResponse> RefreshNewTokenAsync();
    Task RegisterArtistAsync(ArtistRegisterRequest registerRequest);
    Task RegisterListenerAsync(ListenerRegisterRequest registerRequest);
    Task ResendOtpAsync(string email);
    Task VerifyOtpAsync(string email, string providedOtp);
    
    // Password Reset Methods
    Task ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest);
    Task ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest);
    
    // Change Password Method
    Task ChangePasswordAsync(ChangePasswordRequest changePasswordRequest);
}
