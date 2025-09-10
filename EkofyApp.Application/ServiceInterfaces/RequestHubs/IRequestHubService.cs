using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.RequestHubs
{
    public interface IRequestHubService
    {
        Task<bool> CreateRequestAsync(RequestCreatingRequest request);
        IQueryable<RequestHub> GetRequestsQueryable();
        Task SendRequestCommentAsync(CreateRequestCommentRequest commentRequest);
        Task<bool> UpdateRequestAsync(RequestUpdatingRequest request);
    }
}
