using EkofyApp.Application.Models.Requests;
using EkofyApp.Application.ServiceInterfaces.Requests;

namespace EkofyApp.Api.GraphQL.Mutation.Requests
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class RequestMutation(IRequestService requestService)
    {
        private readonly IRequestService _requestService = requestService;

        public async Task<bool> SendRequest(CreateDirectRequest request, bool isDirectRequest = false)
        {
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
