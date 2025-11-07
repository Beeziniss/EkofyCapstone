using EkofyApp.Application.Models.PackageOrders;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.PackageOrders;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.PackageOrders
{
    public sealed class PackageOrderService(IUnitOfWork unitOfWork, IStripeService stripeService) : IPackageOrderService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IStripeService _stripeService = stripeService;
        public IQueryable<PackageOrder> GetPackageOrders()
        {
            return _unitOfWork.GetCollection<PackageOrder>().AsQueryable();
        }

        //public async Task<bool> BuyArtistPackageAsync(string artistPackageId, string userId)
        //{
        //    // Implementation for buying an artist package
        //    return true;
        //}

        public async Task<bool> SubmitDeliverytAsync(SubmitDeliveryRequest request)
        {
            PackageOrder packageOrder = _unitOfWork.GetCollection<PackageOrder>()
                .Find(po => po.Id == request.PackageOrderId && po.Status == PackageOrderStatus.InProgress)
                .Project<PackageOrder>(Builders<PackageOrder>.Projection
                    .Include(po => po.Deliveries)
                    .Include(po => po.RevisionCount))
                .FirstOrDefault();

            if (packageOrder == null)
            {
                throw new NotFoundCustomException("The order of request is not found!");
            }

            //get last revision number of deliveries
            int lastRevisionNumber = packageOrder.Deliveries.Count > 0
                ? packageOrder.Deliveries.Max(d => d.RevisionNumber)
                : 0;

            if (packageOrder.RevisionCount > lastRevisionNumber)
            {
                throw new BadRequestCustomException("You have used all your revisions!");
            }

            // Create new delivery
            var newDelivery = new PackageOrderDelivery
            {
                DeliveryFileUrl = request.DeliveryFileUrl,
                Notes = request.Notes,
                DeliveredAt = HelperMethod.GetUtcPlus7TimeOffset(),
                RevisionNumber = lastRevisionNumber + 1
            };

            // Update the package order with the new delivery
            var update = Builders<PackageOrder>.Update
                .Push(po => po.Deliveries, newDelivery);

            var result = await _unitOfWork.GetCollection<PackageOrder>()
                .UpdateOneAsync(po => po.Id == request.PackageOrderId, update);

            //TODO: gửi notification cho client...

            return result.ModifiedCount > 0;
        }

        public async Task<bool> SendRedoRequest(RedoRequest request)
        {
            //gộp 2 filter lại với nhau để tìm đúng package order và đúng revision
            var filter = Builders<PackageOrder>.Filter.And(
                            Builders<PackageOrder>.Filter.Eq(po => po.Id, request.PackageOrderId),
                            Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                            Builders<PackageOrder>.Filter.ElemMatch(po => po.Deliveries, d => d.RevisionNumber == request.RevisionNumber)
);

            //cập nhật feedback và requestedAt cho delivery tương ứng
            var update = Builders<PackageOrder>.Update
                .Set(po => po.Deliveries[-1].ClientFeedback, request.ClientFeedback)
                .Set(po => po.Deliveries[-1].RequestedAt, HelperMethod.GetUtcPlus7TimeOffset());

            var result = await _unitOfWork.GetCollection<PackageOrder>()
                .UpdateOneAsync(filter, update);

            //TODO: gửi notification cho artist...

            return result.ModifiedCount > 0;
        }

        public async Task<bool> ApproveAndCloseRequest(string packageOrderId)
        {
            // find and update package order. If not found, return false
            var update = Builders<PackageOrder>.Update
                .Set(po => po.Status, PackageOrderStatus.Completed)
                .Set(po => po.CompletedAt, HelperMethod.GetUtcPlus7TimeOffset());

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(
                po => po.Id == packageOrderId && po.Status == PackageOrderStatus.InProgress,
                update);

            //TODO: CHIA TIỀN Ở ĐÂY CHĂNG???
            //_stripeService.EscrowReleaseAsync(packageOrderId)

            return result.ModifiedCount > 0;
        }

        //FOR BACKGROUND JOB
        public async Task ApproveDeliveryAutomatically()
        {
            //tự động duyệt các delivery đã quá hạn 3 ngày mà client không phản hồi
            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                Builders<PackageOrder>.Filter.ElemMatch(po => po.Deliveries,
                         d => d.RequestedAt != null &&
                         d.RequestedAt <= HelperMethod.GetUtcPlus7TimeOffset().AddDays(-3) &&
                         d.ClientFeedback == null)
            );
            var update = Builders<PackageOrder>.Update
                .Set(po => po.Deliveries[-1].ClientFeedback, "This request working is closed and approved automatically by the system! (approve expire > 3 days).")
                .Set(po => po.CompletedAt, HelperMethod.GetUtcPlus7TimeOffset())
                .Set(po => po.Status, PackageOrderStatus.Completed);
            await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(filter, update);

            //TODO: REFUND HERRE
            //_stripeService.EscrowReleaseAsync(packageOrderId)
        }
    }
}
