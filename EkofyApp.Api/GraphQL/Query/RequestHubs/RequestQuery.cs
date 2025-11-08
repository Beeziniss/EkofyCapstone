using EkofyApp.Application.ServiceInterfaces.Requests;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;
using Microsoft.AspNetCore.Authorization;

namespace EkofyApp.Api.GraphQL.Query.RequestHubs;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class RequestQuery(IRequestService requestHubService)
{
    private readonly IRequestService _requestHubService = requestHubService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<Request> GetRequests()
    {
        return _requestHubService.GetRequestsQueryable();
    }

    [AllowAnonymous]
    public async Task<Request?> GetRequestDetailByIdAsync(string requestId)
    {
        return await _requestHubService.GetRequestByIdAsync(requestId);
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Track>]
    public IQueryable<Request> SearchRequests(string searchTerm, bool isIndividual)
    {
        return _requestHubService.SearchRequests(searchTerm, isIndividual);
    }

    [AuthorizeRoles(HelperRoleBase.ListenerRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    public IQueryable<Request> GetOwnRequests()
    {
        return _requestHubService.GetOwnRequestsAsync();
    }
}
