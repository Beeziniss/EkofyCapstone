using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.Models.UserFollows;
using EkofyApp.Application.ServiceInterfaces.Artists;
using EkofyApp.Application.ServiceInterfaces.Users;

namespace EkofyApp.Api.GraphQL.Mutation.Artists;

[ExtendObjectType<MutationInitialization>]
[MutationType]
public class ArtistMutation(IArtistService artistService, IUserService userService)
{
    private readonly IArtistService _artistService = artistService;
    private readonly IUserService _userService = userService;

    public async Task<bool> RegisterArtistManualAsync(CreateArtistRequest createArtistRequest)
    {
        return await _artistService.CreateArtistAsync(createArtistRequest);
    }

    public async Task<bool> UpdateProfileAsync(UpdateArtistRequest updateArtistRequest)
    {
        await _artistService.UpdateProfileAsync(updateArtistRequest);
        return true;
    }

    #region Artist Registration Approval
    public async Task<bool> ApproveArtistRegistrationAsync(ArtistRegistrationApprovalRequest request)
    {
        await _artistService.ApproveArtistRegistrationAsync(request);
        return true;
    }

    public async Task<bool> RejectArtistRegistrationAsync(ArtistRegistrationApprovalRequest request)
    {
        await _artistService.RejectArtistRegistrationAsync(request);
        return true;
    }
    #endregion

    #region Follows
    public async Task<bool> FollowUserAsync(FollowUserRequest request)
    {
        await _userService.FollowUserAsync(request);
        return true;
    }

    public async Task<bool> UnfollowUserAsync(UnfollowUserRequest request)
    {
        await _userService.UnfollowUserAsync(request);
        return true;
    }
    #endregion
}
