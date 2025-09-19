using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Listeners;
public interface IListenerService
{
    IQueryable<Listener> GetListeners();
}
