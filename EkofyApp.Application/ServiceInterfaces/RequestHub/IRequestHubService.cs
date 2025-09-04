using EkofyApp.Application.Models.RequestHub;

namespace EkofyApp.Application.ServiceInterfaces.RequestHub
{
    public interface IRequestHubService
    {
        Task CreateRequestAsync(RequestCreatingRequest requestModel);
    }
}
