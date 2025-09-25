using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Listeners;
using EkofyApp.Domain.Entities;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Listeners;
public sealed class ListenerService(IUnitOfWork unitOfWork) : IListenerService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Listener> GetListeners()
    {
        return _unitOfWork.GetCollection<Listener>().AsQueryable();
    }
}
