using EkofyApp.Application.Models.PackageOrders;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.PackageOrders;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Hangfire;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace EkofyApp.Infrastructure.Services.PackageOrders
{
    [Queue("request")]
    public sealed class PackageOrderService(IUnitOfWork unitOfWork, IStripeService stripeService) : IPackageOrderService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IStripeService _stripeService = stripeService;
        public IQueryable<PackageOrder> GetPackageOrders()
        {
            return _unitOfWork.GetCollection<PackageOrder>().AsQueryable();
        }

        public async Task<bool> AcceptRequestByArtist(string packageOrderId)
        {
            //lấy ra xem order này có được phép cập nhật không
            var packageOrder = await _unitOfWork.GetCollection<PackageOrder>()
                                    .Find(po => po.Id == packageOrderId && po.Status != PackageOrderStatus.Paid)
                                    .FirstOrDefaultAsync()
                              ?? throw new NotFoundCustomException("This package order is not found!");


            //cập nhật trạng thái lại
            var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.InProgress);

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == packageOrderId, update);

            //job chạy khi bắt đầu in progress
            BackgroundJob.Schedule<PackageOrderService>(service => service.SolveOverdueAutomatically(), packageOrder.Deadline);

            return result.ModifiedCount > 0;
        }

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

            if (!string.IsNullOrEmpty(packageOrder.BackgroundJobId))
            {
                BackgroundJob.Delete(packageOrder.BackgroundJobId);
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

            // cho 1 job chạy khi submit thành công
            string jobId = BackgroundJob.Schedule<PackageOrderService>(service => service.ApproveDeliveryAutomatically(request.PackageOrderId), HelperMethod.GetUtcPlus7TimeOffset().AddDays(3));

            update = update.Set(po => po.BackgroundJobId, jobId);

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

            //NOTE: CHIA TIỀN CHO ARTIST Ở ĐÂY -- NOT DONE
            await _stripeService.EscrowReleaseAsync(packageOrderId);

            return result.ModifiedCount > 0;
        }

        //INFO: dành cho người tạo request khi thay đổi trạng thái của đơn đã dặt
        public async Task<bool> SwitchStatusByRequestor(ChangeOrderStatusRequest request)
        {
            var orderPackage = await _unitOfWork.GetCollection<PackageOrder>()
                                          .Find(po => po.Id == request.Id &&
                                                      (po.Status == PackageOrderStatus.InProgress ||
                                                      po.Status == PackageOrderStatus.Paid))
                                          .Project<PackageOrder>(Builders<PackageOrder>.Projection
                                            .Include(po => po.Status)
                                            .Include(po => po.PaymentTransactionId))
                                          .FirstOrDefaultAsync()
                               ?? throw new BadRequestCustomException("You can not do any action for this request due to in progress or complete!");

            //check ở đây xem là đã vô làm việc chưa? Nếu chưa thì refund 100%
            if (orderPackage.Status == PackageOrderStatus.Paid && request.Status == PackageOrderStatus.Cancelled)
            {
                //lấy payment intend id từ transaction
                var transaction = await _unitOfWork.GetCollection<PaymentTransaction>()
                                          .Find(pt => pt.Id == orderPackage.PaymentTransactionId)
                                          .Project<PaymentTransaction>(Builders<PaymentTransaction>.Projection
                                            .Include(pti => pti.Amount)
                                            .Include(pti => pti.StripePaymentId)
                                           )
                                          .FirstOrDefaultAsync()
                            ?? throw new NotFoundCustomException("Oops, we can not find your transaction for this order!");

                //REFUND HERE -- vì ở đây refund 100% nên ko cần chia nhỏ tiền ra
                await _stripeService.RefundAsync(transaction.StripePaymentId, transaction.Amount, RefundReasonType.requested_by_customer);

                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Completed);
                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }


            //nếu đã làm việc thì sẽ chuyển qua cho mod xử lý
            if (orderPackage.Status == PackageOrderStatus.InProgress && request.Status == PackageOrderStatus.Refund)
            {
                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Refund);
                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }

            // trường hợp refund nhưng hủy trước khi mod duyệt
            if (orderPackage.Status == PackageOrderStatus.Refund && request.Status == PackageOrderStatus.InProgress)
            {
                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.InProgress);
                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }
            return false;
        }

        // FOR MOD
        public async Task<bool> RefundPartially(PackageOrderRefundRequest request)
        {
            // 1 là cho refund và thực hiện refund
            var orderPackage = await _unitOfWork.GetCollection<PackageOrder>()
                                          .Find(po => po.Id == request.Id &&
                                                      po.Status == PackageOrderStatus.Refund)
                                          .Project<PackageOrder>(Builders<PackageOrder>.Projection
                                            .Include(po => po.Status)
                                            .Include(po => po.PaymentTransactionId))
                                          .FirstOrDefaultAsync()
                               ?? throw new NotFoundCustomException("Can not find order!");

            //lấy payment intend id từ transaction
            var transaction = await _unitOfWork.GetCollection<PaymentTransaction>()
                                      .Find(pt => pt.Id == orderPackage.PaymentTransactionId)
                                      .Project<PaymentTransaction>(Builders<PaymentTransaction>.Projection
                                        .Include(pti => pti.Amount)
                                        .Include(pti => pti.StripePaymentId)
                                       )
                                      .FirstOrDefaultAsync()
                        ?? throw new NotFoundCustomException("Oops, we can not find your transaction for this order!");

            //REFUND HERE -- vì ở đây refund 100% nên ko cần chia nhỏ tiền ra
            await _stripeService.RefundAsync(transaction.StripePaymentId, Math.Ceiling(transaction.Amount * request.RequestorPercentageAmount / 100), RefundReasonType.requested_by_customer);

            Thread.Sleep(5000);

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Completed));

            //Giải ngân do công việc đã đóng và đã refund *******************************************************************
            //await _stripeService.EscrowReleaseAsync(request.Id);

            return result.ModifiedCount > 0;
        }



        #region FOR BACKGROUND JOB
        // Ở ĐÂY CHỈ THỰC HIỆN BACKGROUND JOB VÀO MỖI 12H SÁNG VÀ TRƯA ĐỂ TRÁNH CẬP NHẬT NHIỀU LÊN DB

        // Nếu sau 3 NGÀY sau khi artist submit file mà requestor chưa approve hay chuyển trạng thái thì sẽ tự động Approve và chuyển tiền
        private async Task ApproveDeliveryAutomatically(string packageOrderId)
        {
            //tự động duyệt các delivery đã quá hạn 3 ngày mà client không phản hồi
            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                Builders<PackageOrder>.Filter.Gt(po => po.Deadline, HelperMethod.GetUtcPlus7TimeOffset()),
                Builders<PackageOrder>.Filter.ElemMatch(po => po.Deliveries,
                         d => d.RequestedAt != null &&
                         d.RequestedAt <= HelperMethod.GetUtcPlus7TimeOffset().AddDays(-3) &&
                         d.ClientFeedback == null)
            );
            var update = Builders<PackageOrder>.Update
                .Set(po => po.Deliveries[-1].ClientFeedback, "This request working is closed and approved automatically by the system! (Because the requestor didn't approve over 3 days).")
                .Set(po => po.CompletedAt, HelperMethod.GetUtcPlus7TimeOffset())
                .Set(po => po.Status, PackageOrderStatus.Completed);
            await _unitOfWork.GetCollection<PackageOrder>().UpdateManyAsync(filter, update);
            // TODO: chuyển tiền thẳng hết luôn!! ******************************************************************************
            await _stripeService.EscrowReleaseAsync(packageOrderId);
            // có lỗi thì thông báo HERE!
        }

        //NẾU NHƯ SAU DEADLINE MÀ VẪN CHƯA HOÀN THÀNH CÔNG VIỆC THÌ TỰ ĐỘNG ĐƯA VAOF DANH SÁCH CHO MOD XỬ LÝ
        public async Task SolveOverdueAutomatically()
        {
            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                Builders<PackageOrder>.Filter.Lt(po => po.Deadline, HelperMethod.GetUtcPlus7TimeOffset())
                );

            var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Refund);
            await _unitOfWork.GetCollection<PackageOrder>().UpdateManyAsync(filter, update);
        }
        #endregion
    }
}
