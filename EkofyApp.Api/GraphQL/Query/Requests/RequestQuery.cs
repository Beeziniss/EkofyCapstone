using EkofyApp.Application.ServiceInterfaces.Requests;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;
using Microsoft.AspNetCore.Authorization;

namespace EkofyApp.Api.GraphQL.Query.Requests;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public class RequestQuery(IRequestService requestService)
{
    private readonly IRequestService _requestService = requestService;

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Request>]
    public IQueryable<Request> GetRequests()
    {
        return _requestService.GetRequestsQueryable();
    }

    [AllowAnonymous]
    public async Task<Request?> GetRequestDetailByIdAsync(string requestId)
    {
        return await _requestService.GetRequestByIdAsync(requestId);
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Request>]
    public IQueryable<Request> SearchRequests(string searchTerm, bool isIndividual)
    {
        return _requestService.SearchRequests(searchTerm, isIndividual);
    }

    [AuthorizeRoles(HelperRoleBase.ListenerRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Request>]
    public IQueryable<Request> GetOwnRequests()
    {
        return _requestService.GetOwnRequestsAsync();
    }
}
