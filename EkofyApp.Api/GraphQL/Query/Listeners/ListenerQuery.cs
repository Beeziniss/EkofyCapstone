using EkofyApp.Application.ServiceInterfaces.Listeners;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Api.GraphQL.Query.Listeners;

[ExtendObjectType(typeof(QueryInitialization))]
[QueryType]
public sealed class ListenerQuery(IListenerService listenerService)
{
    private readonly IListenerService _listenerService = listenerService;

    public IQueryable<Listener> GetListeners()
    {
        return _listenerService.GetListeners();
    }
}
