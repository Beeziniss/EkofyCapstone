using EkofyApp.Application.ServiceInterfaces.Listeners;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Utils;
using HotChocolate.Data;

namespace EkofyApp.Api.GraphQL.Query.Listeners;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ListenerQuery(IListenerService listenerService)
{
    private readonly IListenerService _listenerService = listenerService;

    [AuthorizeRoles(HelperRoleBase.FullRoles)]
    [UseOffsetPaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting<Listener>]
    public IQueryable<Listener> GetListeners()
    {
        return _listenerService.GetListeners();
    }
}
