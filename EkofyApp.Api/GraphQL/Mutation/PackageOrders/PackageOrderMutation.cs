using EkofyApp.Application.Models.PackageOrders;
using EkofyApp.Application.Models.Reviews;
using EkofyApp.Application.ServiceInterfaces.PackageOrders;

namespace EkofyApp.Api.GraphQL.Mutation.PackageOrders
{
    public sealed class PackageOrderMutation(IPackageOrderService packageOrderService)
    {
        private readonly IPackageOrderService _packageOrderService = packageOrderService;

        public async Task<bool> SubmitDeliverytAsync(SubmitDeliveryRequest request)
        {
            await _packageOrderService.SubmitDeliverytAsync(request);
            return true;
        }

        public async Task<bool> ApproveDeliveryAsync(string packageOrderId)
        {
            await _packageOrderService.ApproveAndCloseRequest(packageOrderId);
            return true;
        }

        public async Task<bool> SendRedoRequestAsync(RedoRequest request)
        {
            await _packageOrderService.SendRedoRequest(request);
            return true;
        }

        public async Task<bool> AcceptRequestByArtistAsync(string packageOrderId)
        {
            await _packageOrderService.AcceptRequestByArtist(packageOrderId);
            return true;
        }

        public async Task<bool> SwitchStatusByRequestorAsync(ChangeOrderStatusRequest request)
        {
            await _packageOrderService.SwitchStatusByRequestor(request);
            return true;
        }

        public async Task<bool> RefundPartiallyAsync(PackageOrderRefundRequest request)
        {
            await _packageOrderService.RefundPartially(request);
            return true;
        }

        public async Task<bool> CreateReviewAsync(CreateReviewRequest createReviewRequest)
        {
            await _packageOrderService.CreateReviewAsync(createReviewRequest);
            return true;
        }

        public async Task<bool> UpdateReviewAsync(UpdateReviewRequest updateReviewRequest)
        {
            await _packageOrderService.UpdateReviewAsync(updateReviewRequest);
            return true;
        }

        public async Task<bool> DeleteReviewHardAsync(string reviewId)
        {
            await _packageOrderService.DeleteReviewHardAsync(reviewId);
            return true;
        }
    }
}
