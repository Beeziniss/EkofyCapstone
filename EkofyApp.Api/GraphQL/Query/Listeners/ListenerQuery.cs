using EkofyApp.Application.ServiceInterfaces.Listeners;
using EkofyApp.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Listeners;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ListenerQuery(IListenerService listenerService)
{
    private readonly IListenerService _listenerService = listenerService;

    //[AuthorizeRoles(HelperRoleBase.FullRoles)]
    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Listener>]
    public IQueryable<Listener> GetListeners()
    {
        return _listenerService.GetListeners();
    }

    [AllowAnonymous]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Listener>]
    public IQueryable<Listener> SearchListeners(string displayName)
    {
        return _listenerService.SearchListeners(displayName);
    }
}
