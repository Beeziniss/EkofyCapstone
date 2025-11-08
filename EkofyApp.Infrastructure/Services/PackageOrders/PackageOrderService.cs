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
using MongoDB.Driver.Linq;

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

        //hàm này dùng chung cho cả 2
        public async Task<bool> AcceptRequestByArtist(string packageOrderId)
        {
            //lấy ra xem order này có được phép cập nhật không
            bool isOrderExist = await _unitOfWork.GetCollection<PackageOrder>().Find(po => po.Id == packageOrderId && po.Status != PackageOrderStatus.Paid).AnyAsync();

            if (!isOrderExist)
            {
                throw new BadRequestCustomException("This request haven't been paid, not found or close!");
            }

            //cập nhật trạng thái lại
            var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.InProgress);

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == packageOrderId, update);

            return result.ModifiedCount > 0;
        }


        #region FOR DIRECT REQUEST ONLY

        #endregion

        #region FOR REQUEST HUB ONLY

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

        public async Task<bool> SwitchStatusByRequestor(ChangeOrderStatusRequest request)
        {
            ProjectionDefinition<PackageOrder> projection = Builders<PackageOrder>.Projection.Include(po => po.Status);

            var orderPackage = _unitOfWork.GetCollection<PackageOrder>()
                                          .Find(po => po.Id == request.Id && 
                                                      (po.Status == PackageOrderStatus.InProgress ||
                                                      po.Status == PackageOrderStatus.Paid))
                                          .Project<PackageOrder>(projection)
                                          .FirstOrDefault()
                               ?? throw new BadRequestCustomException("You can not do any action for this request!");

            //check ở đây xem là đã vô làm việc chưa? Nếu chưa thì refund 100%
            if(orderPackage.Status == PackageOrderStatus.Paid && request.Status == PackageOrderStatus.Cancelled)
            {
                //REFUND HERE
            }


            //nếu đã làm việc thì sẽ chuyển qua cho mod xử lý
            if(orderPackage.Status == PackageOrderStatus.InProgress && request.Status == PackageOrderStatus.Refund)
            {
                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Refund);
                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }
            return true;
        }

        // FOR MOD
        public async Task<bool> RefundPartially()
        {
            // assign model above and send it to invoke method

            // 1 là cho refund và thực hiện refund
            // 2 là không cho refund, gửi lý do qua thông báo và đặt lại trạng thái về in progress
            return true;
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


        #endregion
    }
}
