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



}
