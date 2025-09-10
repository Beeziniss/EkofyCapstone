using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces.RequestHubs;

namespace EkofyApp.Api.GraphQL.Mutation.RequestHubs
{
    [ExtendObjectType(typeof(MutationInitialization))]
    [MutationType]
    public class RequestMutation(IRequestHubService requestHubService)
    {
        private readonly IRequestHubService _requestHubService = requestHubService;

        public async Task<bool> CreateRequestAsync(RequestCreatingRequest request)
        {
            return await _requestHubService.CreateRequestAsync(request);
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            return await _requestHubService.UpdateRequestAsync(request);
        }
    }
}
