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

    }
}
