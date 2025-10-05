using EkofyApp.Application.Models.Listeners;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Listeners;
public interface IListenerService
{
    IQueryable<Listener> GetListeners();
    Task UpdateProfileAsync(UpdateListenerRequest updateListenerRequest);
}
