using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.RequestHubs
{
    public interface IRequestHubService
    {
        Task<bool> BlockRequestAsync(string requestId);
        Task<bool> CreateRequestAsync(RequestCreatingRequest request);
        Task<RequestHub?> GetRequestByIdAsync(string requestId);
        IQueryable<RequestHub> GetRequestsQueryable();
        IQueryable<RequestHub> SearchRequests(string searchTerm);
        Task<bool> UpdateRequestAsync(RequestUpdatingRequest request);
    }
}
