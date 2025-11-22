using EkofyApp.Application.Models.Requests;
using EkofyApp.Application.ServiceInterfaces.Requests;
using EkofyApp.Application.ServiceInterfaces.Users;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;

namespace EkofyApp.Api.GraphQL.Mutation.Requests
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class RequestMutation(IRequestService requestService, IUserService userService)
    {
        private readonly IRequestService _requestService = requestService;
        private readonly IUserService _userService = userService;

        public async Task<bool> SendRequest(CreateDirectRequest request, bool isDirectRequest = false)
        {
            string action = isDirectRequest ? "sending direct request" : "creating public request";
            bool hasAnyRestriction = await _userService.CheckMultipleRestrictionsAsync(isDirectRequest ? RestrictionAction.SendRequest : RestrictionAction.CreatePublicRequest);
            if (hasAnyRestriction)
            {
                throw new UnauthorizedCustomException($"You are restricted from {action}.");
            }
            return await _requestService.SendRequest(request, isDirectRequest);
        }

        public async Task<bool> ChangeRequestStatusAsync(ChangeStatusRequest request)
        {
            return await _requestService.ChangeRequestStatus(request);
        }

        public async Task<bool> CreatePublicRequestAsync(RequestCreatingRequest request)
        {
            return await _requestService.CreatePublicRequestAsync(request);
        }

        public async Task<bool> UpdatePublicRequestAsync(RequestUpdatingRequest request)
        {
            return await _requestService.UpdatePublicRequestAsync(request);
        }

        public async Task<bool> BlockPublicRequestAsync(string requestId)
        {
            return await _requestService.BlockPublicRequestAsync(requestId);
        }
    }
}
