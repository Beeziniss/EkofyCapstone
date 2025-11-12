using EkofyApp.Application.Models.Requests;
using EkofyApp.Application.ServiceInterfaces.Requests;

namespace EkofyApp.Api.GraphQL.Mutation.RequestHubs
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class RequestMutation(IRequestService requestHubService)
    {
        private readonly IRequestService _requestHubService = requestHubService;

        public async Task<bool> SendRequest(CreateDirectRequest request, bool isDirectRequest = false)
        {
            return await _requestHubService.SendRequest(request, isDirectRequest);
        }

        public async Task<bool> ChangeRequestStatusAsync(ChangeStatusRequest request)
        {
            return await _requestHubService.ChangeRequestStatus(request);
        }

        public async Task<bool> CreateRequestAsync(RequestCreatingRequest request)
        {
            return await _requestHubService.CreatePublicRequestAsync(request);
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            return await _requestHubService.UpdatePublicRequestAsync(request);
        }

        public async Task<bool> BlockRequestAsync(string requestId)
        {
            return await _requestHubService.BlockPublicRequestAsync(requestId);
        }
    }
}
