using EkofyApp.Application.ServiceInterfaces.Authentication;

namespace EkofyApp.Api.GraphQL.Mutation.Authentication
{
    [ExtendObjectType(typeof(MutationInitialization))]
    public class AuthenticationMutation(IAuthentication authenticationService)
    {
        private readonly IAuthentication _authenticationService = authenticationService;

        //public async Task<AuthenticatedUserInfoResponseModel> Login(LoginRequestModel loginModel)
        //{
        //    AuthenticatedUserInfoResponseModel accessToken = await _authenticationService.Authenticate(loginModel);
        //    return accessToken;
        //}
    }
}
