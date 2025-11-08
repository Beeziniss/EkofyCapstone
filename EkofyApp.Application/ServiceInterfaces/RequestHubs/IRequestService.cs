using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.RequestHubs
{
    public interface IRequestService
    {
        Task<bool> BlockRequestAsync(string requestId);
        Task<bool> CreateRequestAsync(RequestCreatingRequest request);
        IQueryable<Request> GetOwnRequestsAsync();
        Task<Request?> GetRequestByIdAsync(string requestId);
        IQueryable<Request> GetRequestsQueryable();
        IQueryable<Request> SearchRequests(string searchTerm, bool isIndividual);
        Task<bool> UpdateRequestAsync(RequestUpdatingRequest request);
    }
}
