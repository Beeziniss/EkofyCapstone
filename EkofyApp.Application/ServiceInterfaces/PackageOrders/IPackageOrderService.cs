using EkofyApp.Application.Models.PackageOrders;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.PackageOrders
{
    public interface IPackageOrderService
    {
        Task<bool> ApproveAndCloseRequest(string packageOrderId);
        IQueryable<PackageOrder> GetPackageOrders();
        Task<bool> SendRedoRequest(RedoRequest request);
        Task<bool> SubmitDeliverytAsync(SubmitDeliveryRequest request);
    }
}
