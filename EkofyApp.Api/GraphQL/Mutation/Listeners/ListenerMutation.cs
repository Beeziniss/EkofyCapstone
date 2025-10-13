using EkofyApp.Application.Models.Listeners;
using EkofyApp.Application.ServiceInterfaces.Listeners;

namespace EkofyApp.Api.GraphQL.Mutation.Listeners;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class ListenerMutation(IListenerService listenerService)
{
    private readonly IListenerService _listenerService = listenerService;

    public async Task<bool> UpdateProfileAsync(UpdateListenerRequest updateListenerRequest)
    {
        await _listenerService.UpdateProfileAsync(updateListenerRequest);
        return true;
    }
}
