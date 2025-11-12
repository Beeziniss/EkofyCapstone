using EkofyApp.Application.Models.PackageOrders;
using EkofyApp.Application.Models.Reviews;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.PackageOrders
{
    public interface IPackageOrderService
    {
        Task<bool> AcceptRequestByArtist(string packageOrderId);
        Task<bool> ApproveAndCloseRequest(string packageOrderId);
        Task<bool> CheckClientReviewedPackageOrderAsync(string clientId, string packageOrderId);
        Task CreateReviewAsync(CreateReviewRequest createReviewRequest);
        Task DeleteReviewHardAsync(string packageOrderId);
        Task<ReviewResponse> GetAverageRatingBaseOnPackageAsync(string packageId);
        IQueryable<PackageOrder> GetPackageOrders();
        Task<bool> SendRedoRequest(RedoRequest request);
        Task<bool> SubmitDeliverytAsync(SubmitDeliveryRequest request);
        Task UpdateReviewAsync(UpdateReviewRequest updateReviewRequest);
    }
}
