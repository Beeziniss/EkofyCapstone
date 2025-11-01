using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;
using Microsoft.AspNetCore.Authorization;

namespace EkofyApp.Api.GraphQL.Query.RequestHubs;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class RequestQuery(IRequestHubService requestHubService)
{
    private readonly IRequestHubService _requestHubService = requestHubService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<RequestHub> GetRequests()
    {
        return _requestHubService.GetRequestsQueryable();
    }

    [AllowAnonymous]
    public async Task<RequestHub?> GetRequestDetailByIdAsync(string requestId)
    {
        return await _requestHubService.GetRequestByIdAsync(requestId);
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<RequestHub> SearchRequests(string searchTerm, bool isIndividual)
    {
        return _requestHubService.SearchRequests(searchTerm, isIndividual);
    }

    [AuthorizeRoles(HelperRoleBase.ListenerRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    public IQueryable<RequestHub> GetOwnRequests()
    {
        return _requestHubService.GetOwnRequestsAsync();
    }
}
