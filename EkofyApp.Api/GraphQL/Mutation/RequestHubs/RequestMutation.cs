using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces.Requests;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Api.GraphQL.Mutation.RequestHubs
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class RequestMutation(IRequestService requestHubService, IUserService userService)
    {
        private readonly IRequestService _requestHubService = requestHubService;
        private readonly IUserService _userService = userService;

        public async Task<bool> CreateRequestAsync(RequestCreatingRequest request)
        {
            bool hasAnyRestriction = await _userService.CheckMultipleRestrictionsAsync(RestrictionAction.CreatePublicRequest);
            if (hasAnyRestriction)
            {
                throw new UnauthorizedAccessException("You are restricted from creating public request.");
            }

            return await _requestHubService.CreateRequestAsync(request);
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            return await _requestHubService.UpdateRequestAsync(request);
        }

        public async Task<bool> BlockRequestAsync(string requestId)
        {
            return await _requestHubService.BlockRequestAsync(requestId);
        }
    }
}
