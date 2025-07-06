using EkofyApp.Application.ServiceInterfaces.Authentication;

namespace EkofyApp.Api.GraphQL.Mutation.Authentication
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class AuthenticationMutation(IAuthenticationService authenticationService)
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;

        //public async Task<AuthenticatedUserInfoResponseModel> Login(LoginRequestModel loginModel)
        //{
        //    AuthenticatedUserInfoResponseModel accessToken = await _authenticationService.Authenticate(loginModel);
        //    return accessToken;
        //}
    }
}
