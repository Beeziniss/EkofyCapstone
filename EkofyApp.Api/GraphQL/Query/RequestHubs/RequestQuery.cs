using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.RequestHubs;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class RequestQuery(IRequestHubService requestHubService)
{
    private readonly IRequestHubService _requestHubService = requestHubService;

    public IQueryable<RequestHub> GetRequests()
    {
        return _requestHubService.GetRequestsQueryable();
    }

    public async Task<RequestHub?> GetRequestByIdAsync(string requestId)
    {
        return await _requestHubService.GetRequestByIdAsync(requestId);
    }

    public IQueryable<RequestHub> SearchRequests(string searchTerm)
    {
        return _requestHubService.SearchRequests(searchTerm);
    }
}
