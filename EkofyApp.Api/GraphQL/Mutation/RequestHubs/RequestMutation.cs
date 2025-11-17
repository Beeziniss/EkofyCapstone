using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces.Requests;

namespace EkofyApp.Api.GraphQL.Mutation.RequestHubs
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class RequestMutation(IRequestService requestHubService)
    {
        private readonly IRequestService _requestHubService = requestHubService;

        public async Task<bool> CreateRequestAsync(RequestCreatingRequest request)
        {
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
