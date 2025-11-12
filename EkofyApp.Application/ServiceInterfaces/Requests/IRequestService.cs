using EkofyApp.Application.Models.Requests;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Requests
{
    public interface IRequestService
    {
        Task<bool> BlockPublicRequestAsync(string requestId);
        Task<bool> ChangeRequestStatus(ChangeStatusRequest request);
        Task<bool> CreatePublicRequestAsync(RequestCreatingRequest request);
        IQueryable<Request> GetOwnRequestsAsync();
        Task<Request?> GetRequestByIdAsync(string requestId);
        IQueryable<Request> GetRequestsQueryable();
        IQueryable<Request> SearchRequests(string searchTerm, bool isIndividual);
        Task<bool> SendRequest(CreateDirectRequest request, bool isDirectRequest = false);
        Task<bool> UpdatePublicRequestAsync(RequestUpdatingRequest request);
    }
}
