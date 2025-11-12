using EkofyApp.Application.ServiceInterfaces.PackageOrders;
using EkofyApp.Application.Models.PackageOrders;

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

    }
}
