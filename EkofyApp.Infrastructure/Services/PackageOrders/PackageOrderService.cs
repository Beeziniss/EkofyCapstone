using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.Models.PackageOrders;
using EkofyApp.Application.Models.Reviews;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.PackageOrders;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using EkofyApp.Domain.EmbeddedDocuments;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;

namespace EkofyApp.Infrastructure.Services.PackageOrders
{
    [Queue("request")]
    public sealed class PackageOrderService(IUnitOfWork unitOfWork, IStripeService stripeService, IHttpContextAccessor httpContextAccessor, IHubContext<NotificationHub> hubContext) : IPackageOrderService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IStripeService _stripeService = stripeService;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        public IQueryable<PackageOrder> GetPackageOrders()
        {
            return _unitOfWork.GetCollection<PackageOrder>().AsQueryable();
        }

        public async Task<bool> AcceptRequestByArtist(string packageOrderId)
        {
            //lấy ra xem order này có được phép cập nhật không
            var packageOrder = await _unitOfWork.GetCollection<PackageOrder>()
                                    .Find(po => po.Id == packageOrderId && po.Status == PackageOrderStatus.Paid)
                                    .FirstOrDefaultAsync()
                              ?? throw new NotFoundCustomException("This package order is not found!");

            DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();

            //cập nhật trạng thái lại
            var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.InProgress)
                                                      .Set(po => po.StartedAt, now);

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == packageOrderId, update);

            var deadline = now.AddDays(packageOrder.Duration);

            //job chạy khi bắt đầu in progress
            BackgroundJob.Schedule<PackageOrderService>(service => service.SolveOverdueAutomatically(packageOrderId, deadline), deadline);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> SubmitDeliveryAsync(SubmitDeliveryRequest request)
        {
            PackageOrder packageOrder = _unitOfWork.GetCollection<PackageOrder>()
                .Find(po => po.Id == request.PackageOrderId && po.Status == PackageOrderStatus.InProgress)
                .Project<PackageOrder>(Builders<PackageOrder>.Projection
                    .Include(po => po.Deliveries)
                    .Include(po => po.RevisionCount)
                    .Include(po => po.ProviderId)
                    .Include(po => po.ClientId)
                    .Include(po => po.StartedAt)
                    .Include(po => po.Duration)
                    .Include(po => po.FreezedTime)
                    .Include(po => po.ApprovedAutoJobId)

                    )
                .FirstOrDefault() ?? throw new NotFoundCustomException("The order of request is not found!");

            //get last revision number of deliveries
            int lastRevisionNumber = packageOrder.Deliveries.Count > 0
                ? packageOrder.Deliveries.Max(d => d.RevisionNumber)
                : -1;

            if (packageOrder.RevisionCount > lastRevisionNumber)
            {
                throw new BadRequestCustomException("You have used all your revisions!");
            }

            if (!string.IsNullOrEmpty(packageOrder.ApprovedAutoJobId))
            {
                BackgroundJob.Delete(packageOrder.ApprovedAutoJobId);
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
                .Push(po => po.Deliveries, newDelivery)
                .Inc(po => po.RevisionCount, 1);


            DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();
            DateTimeOffset deadline = packageOrder.StartedAt!.Value.AddDays(packageOrder.Duration) + packageOrder.FreezedTime;

            // cho 1 job chạy khi submit thành công
            string jobId = BackgroundJob.Schedule<PackageOrderService>(service => service.ApproveDeliveryAutomatically(request.PackageOrderId, deadline, now.AddDays(3)), now.AddDays(3));

            update = update.Set(po => po.ApprovedAutoJobId, jobId);

            var result = await _unitOfWork.GetCollection<PackageOrder>()
                        .UpdateOneAsync(po => po.Id == request.PackageOrderId, update);

            //TODO: gửi notification cho client...
            string avatar = await _unitOfWork.GetCollection<Artist>()
                .Find(u => u.UserId == packageOrder.ProviderId)
                .Project(u => u.AvatarImage)
                .FirstOrDefaultAsync() ?? string.Empty;
            string content = $"New delivery #{newDelivery.RevisionNumber} has been submitted for your order {request.PackageOrderId}. Please review it.";
            await _hubContext.Clients.User(packageOrder.ClientId).SendAsync("ReceiveNotification", new NotificationResponse
            {
                Content = content,
                Avatar = avatar,
            });

            await _unitOfWork.GetCollection<Notification>()
                .InsertOneAsync(new Notification
                {
                    ActorId = packageOrder.ProviderId,
                    TargetId = packageOrder.ClientId,
                    Content = content,
                    Action = NotificationActionType.Other,
                    RelatedId = request.PackageOrderId,
                    RelatedType = NotificationRelatedType.Order,
                    Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/orders/{request.PackageOrderId}/submission"
                });

            return result.ModifiedCount > 0;
        }

        public async Task<bool> SendRedoRequest(RedoRequest request)
        {
            PackageOrder packageOrder = await _unitOfWork.GetCollection<PackageOrder>()
                .Find(po => po.Id == request.PackageOrderId)
                .Project<PackageOrder>(Builders<PackageOrder>.Projection
                    .Include(po => po.ProviderId)
                    .Include(po => po.Deliveries)
                    .Include(po => po.ClientId))
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("The order of request is not found!");

            if (packageOrder == null || packageOrder.Deliveries.Count == 0)
                return false;

            int lastIndex = packageOrder.Deliveries.Count - 1;

            var update = Builders<PackageOrder>.Update
                .Set($"Deliveries.{lastIndex}.ClientFeedback", request.ClientFeedback)
                .Set($"Deliveries.{lastIndex}.RequestedAt", HelperMethod.GetUtcPlus7TimeOffset());

            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Id, request.PackageOrderId),
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress)
            );

            var result = await _unitOfWork.GetCollection<PackageOrder>()
                .UpdateOneAsync(filter, update);

            //TODO: gửi notification cho artist...
            string avatar = await _unitOfWork.GetCollection<Listener>()
                .Find(u => u.UserId == packageOrder.ProviderId)
                .Project(u => u.AvatarImage)
                .FirstOrDefaultAsync() ?? string.Empty;
            string content = $"A redo request has been sent for your delivery #{request.RevisionNumber} on order {request.PackageOrderId}. Please review the feedback.";
            await _hubContext.Clients.User(packageOrder.ProviderId).SendAsync("ReceiveNotification", new NotificationResponse
            {
                Content = content,
                Avatar = avatar,
            });

            await _unitOfWork.GetCollection<Notification>()
                .InsertOneAsync(new Notification
                {
                    ActorId = packageOrder.ClientId,
                    TargetId = packageOrder.ProviderId,
                    Content = content,
                    Action = NotificationActionType.Other,
                    RelatedId = request.PackageOrderId,
                    RelatedType = NotificationRelatedType.Order,
                    Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/orders/{request.PackageOrderId}/submission"
                });

            return result.ModifiedCount > 0;
        }

        public async Task<bool> ApproveAndCloseRequest(string packageOrderId)
        {
            // find and update package order. If not found, return false
            var update = Builders<PackageOrder>.Update
                .Set(po => po.Status, PackageOrderStatus.Completed)
                .Set(po => po.CompletedAt, HelperMethod.GetUtcPlus7TimeOffset());

            //NOTE: CHIA TIỀN CHO ARTIST Ở ĐÂY
            BackgroundJob.Enqueue<IStripeService>(service => service.EscrowReleaseAsync(packageOrderId, null));

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(
                po => po.Id == packageOrderId && po.Status == PackageOrderStatus.InProgress,
                update);

            return result.ModifiedCount > 0;
        }

        //INFO: dành cho người tạo request khi thay đổi trạng thái của đơn đã dặt
        public async Task<bool> SwitchStatusByRequestorAsync(ChangeOrderStatusRequest request)
        {
            var orderPackage = await _unitOfWork.GetCollection<PackageOrder>()
                                          .Find(po => po.Id == request.Id &&
                                                      (po.Status == PackageOrderStatus.InProgress ||
                                                      po.Status == PackageOrderStatus.Paid ||
                                                      po.Status == PackageOrderStatus.Disputed))
                                          .Project<PackageOrder>(Builders<PackageOrder>.Projection
                                            .Include(po => po.Status)
                                            .Include(po => po.PaymentTransactionId)
                                            .Include(po => po.StartedAt)
                                            .Include(po => po.Duration)
                                            .Include(po => po.FreezedTime)
                                            .Include(po => po.OverdueJobId)
                                            .Include(po => po.DisputedAt)
                                            .Include(po => po.ProviderId))
                                          .FirstOrDefaultAsync()
                               ?? throw new BadRequestCustomException("You can not do any action for this request due to in progress or complete!");

            //check ở đây xem là đã vô làm việc chưa? Nếu chưa thì refund 100%
            if (orderPackage.Status == PackageOrderStatus.Paid && request.Status == PackageOrderStatus.Disputed)
            {
                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Disputed)
                                                          .Set(po => po.DisputedReason, request.Reason)
                                                          .Set(po => po.DisputedAt, HelperMethod.GetUtcPlus7TimeOffset());
                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }


            //nếu đã làm việc thì sẽ chuyển qua cho mod xử lý
            if (orderPackage.Status == PackageOrderStatus.InProgress && request.Status == PackageOrderStatus.Disputed)
            {
                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Disputed)
                                                          .Set(po => po.DisputedReason, request.Reason)
                                                          .Set(po => po.DisputedAt, HelperMethod.GetUtcPlus7TimeOffset());
                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }

            // trường hợp refund nhưng hủy trước khi mod duyệt hoặc mod hủy
            if (orderPackage.Status == PackageOrderStatus.Disputed && request.Status == PackageOrderStatus.InProgress)
            {
                var now = HelperMethod.GetUtcPlus7TimeOffset();

                //set laị job
                if (!string.IsNullOrEmpty(orderPackage.OverdueJobId))
                {
                    BackgroundJob.Delete(orderPackage.OverdueJobId);
                }

                var deadline = orderPackage.StartedAt!.Value.AddDays(orderPackage.Duration) + orderPackage.FreezedTime;

                var jobId = BackgroundJob.Schedule<PackageOrderService>(service => service.SolveOverdueAutomatically(request.Id, deadline), deadline);

                var allFreezedTime = orderPackage.FreezedTime + now.Subtract(orderPackage.DisputedAt!.Value);

                var update = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.InProgress)
                                                          .Set(po => po.FreezedTime, allFreezedTime)
                                                          .Set(update => update.OverdueJobId, jobId)
                                                          .Set(po => po.UpdatedAt, now);

                var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, update);
                return result.ModifiedCount > 0;
            }
            return false;
        }

        // FOR MOD
        public async Task<bool> RefundPartiallyAndEscrowAsync(PackageOrderRefundRequest request)
        {
            // 1 là cho refund và thực hiện refund
            var orderPackage = await _unitOfWork.GetCollection<PackageOrder>()
                                          .Find(po => po.Id == request.Id &&
                                                      (po.Status == PackageOrderStatus.Disputed))
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

            await _stripeService.RefundAsync(transaction.StripePaymentId!, transaction.Amount * request.RequestorPercentageAmount / 100m, RefundReasonType.requested_by_customer);



            //Giải ngân do công việc đã đóng và đã refund *******************************************************************
            BackgroundJob.Enqueue<IStripeService>(service => service.EscrowReleaseAsync(request.Id, transaction.Amount * request.ArtistPercentageAmount / 100m));

            // Cập nhật service revenue cho Platform
            UpdateDefinition<PlatformRevenue> updatePlatformRevenue = Builders<PlatformRevenue>.Update
                        .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                        .Inc(x => x.RefundAmount, (transaction.Amount * request.RequestorPercentageAmount / 100m))
                        .Inc(x => x.ServicePayoutAmount, transaction.Amount * request.ArtistPercentageAmount / 100m);
            UpdateResult updatePlatformRevenueResult = await _unitOfWork.GetCollection<PlatformRevenue>()
                .UpdateOneAsync(_ => true, updatePlatformRevenue);
            if (updatePlatformRevenueResult.ModifiedCount == 0)
            {
                Log.Error("Cannot update platform revenue after checkout session completed.");
            }

            // Cập nhật service cho Artist
            UpdateDefinition<Artist> updateArtistRevenue = Builders<Artist>.Update
                        .Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                        .Inc(x => x.ServiceEarnings, transaction.Amount * request.ArtistPercentageAmount / 100m);
            UpdateResult updateArtistRevenueResult = await _unitOfWork.GetCollection<Artist>()
                .UpdateOneAsync(x => x.UserId == orderPackage.ProviderId, updateArtistRevenue);
            if (updateArtistRevenueResult.ModifiedCount == 0)
            {
                Log.Error("Cannot update artist revenue after checkout session completed.");
            }

            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(po => po.Id == request.Id, Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Refund));

            return result.ModifiedCount > 0;
        }

        #region Review
        public async Task<ReviewResponse> GetAverageRatingBaseOnPackageAsync(string packageId)
        {
            List<int> reviews = await _unitOfWork.GetCollection<PackageOrder>()
                    .Find(x => x.ArtistPackageId == packageId && x.Review != null)
                    .Project(x => x.Review!.Rating)
                    .ToListAsync();

            if (reviews.Count == 0)
            {
                return new ReviewResponse
                {
                    AverageRating = 0,
                    TotalReviews = 0
                };
            }

            return new ReviewResponse
            {
                AverageRating = Convert.ToInt32(Math.Round(reviews.Average())),
                TotalReviews = reviews.Count
            };
        }

        public async Task CreateReviewAsync(CreateReviewRequest createReviewRequest)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            if (await _unitOfWork.GetCollection<PackageOrder>().Find(x => x.ClientId == userId && x.Id == createReviewRequest.PackageOrderId).AnyAsync())
            {
                throw new ConflictCustomException("You have already reviewed this package order");
            }

            // Tạo review
            UpdateResult updateResult = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(
                x => x.Id == createReviewRequest.PackageOrderId,
                Builders<PackageOrder>.Update
                    .Set(x => x.CreatedAt, HelperMethod.GetUtcPlus7TimeOffset())
                    .Set(x => x.UpdatedAt, null)
                    .Set(x => x.Review, new Review
                    {
                        Rating = createReviewRequest.Rating,
                        Content = createReviewRequest.Content
                    })
            );
            if (updateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Cannot create review");
            }
        }

        public async Task UpdateReviewAsync(UpdateReviewRequest updateReviewRequest)
        {
            // Kiểm tra review có tồn tại không
            if (!await _unitOfWork.GetCollection<PackageOrder>()
                    .Find(x => x.Id == updateReviewRequest.PackageOrderId && x.Review != null)
                    .AnyAsync())
            {
                throw new NotFoundCustomException("Review does not exist");
            }

            List<UpdateDefinition<PackageOrder>> updates = [Builders<PackageOrder>.Update.Set(x => x.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset())];

            if (updateReviewRequest.Rating != null)
            {
                updates.Add(Builders<PackageOrder>.Update.Set(r => r.Review!.Rating, updateReviewRequest.Rating.Value));
            }

            if (updateReviewRequest.Comment != null)
            {
                updates.Add(Builders<PackageOrder>.Update.Set(r => r.Review!.Content, updateReviewRequest.Comment));
            }

            UpdateDefinition<PackageOrder> updateDefinition = Builders<PackageOrder>.Update.Combine(updates);

            UpdateResult updateResult = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(
                r => r.Id == updateReviewRequest.PackageOrderId,
                updateDefinition
            );
            if (updateResult.ModifiedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Cannot update review");
            }
        }

        public async Task DeleteReviewHardAsync(string packageOrderId)
        {
            // Kiểm tra review có tồn tại không
            if (!await _unitOfWork.GetCollection<PackageOrder>()
                        .Find(x => x.Id == packageOrderId && x.Review != null)
                        .AnyAsync())
            {
                throw new NotFoundCustomException("Review does not exist");
            }

            DeleteResult deleteResult = await _unitOfWork.GetCollection<PackageOrder>().DeleteOneAsync(r => r.Id == packageOrderId);
            if (deleteResult.DeletedCount == 0)
            {
                throw new UnprocessableEntityCustomException("Cannot delete review");
            }
        }

        public async Task<bool> CheckClientReviewedPackageOrderAsync(string clientId, string packageOrderId)
        {
            return await _unitOfWork.GetCollection<PackageOrder>()
                .Find(x => x.Id == packageOrderId && x.ClientId == clientId && x.Review != null)
                .AnyAsync();
        }
        #endregion

        #region FOR BACKGROUND JOB
        // Nếu sau 3 NGÀY sau khi artist submit file mà requestor chưa approve hay chuyển trạng thái thì sẽ tự động Approve và chuyển tiền
        public async Task ApproveDeliveryAutomatically(string packageOrderId, DateTimeOffset deadline, DateTimeOffset expireDelivery)
        {
            DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();
            if (deadline <= now)
            {
                var updateWhenExpired = Builders<PackageOrder>.Update.Set(po => po.Status, PackageOrderStatus.Disputed)
                                                          .Set(po => po.DisputedAt, now)
                                                          .Set(po => po.UpdatedAt, now);

                await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(p => p.Id == packageOrderId, updateWhenExpired);
                return;
            }

            if (expireDelivery >= now) return;

            var package = await _unitOfWork.GetCollection<PackageOrder>()
                                            .Find(po => po.Id == packageOrderId && po.Status == PackageOrderStatus.InProgress)
                                            .Project<PackageOrder>(Builders<PackageOrder>.Projection
                                                .Include(po => po.Deliveries))
                                            .FirstOrDefaultAsync();

            if (package == null) return;
            
            PackageOrderDelivery? delivery = package.Deliveries.LastOrDefault(d =>
                                                            d.RequestedAt == null &&
                                                            d.ClientFeedback == null
                                                        );

            if (delivery == null) return;
            //tự động duyệt các delivery đã quá hạn 3 ngày mà client không phản hồi
            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                Builders<PackageOrder>.Filter.Eq(po => po.Id, packageOrderId),
                Builders<PackageOrder>.Filter.ElemMatch(po => po.Deliveries,
                         d => d.RequestedAt == null && d.ClientFeedback == null)
            );
            var update = Builders<PackageOrder>.Update
                .Set("Deliveries.$.ClientFeedback", "This request working is closed and approved automatically by the system! (Because the requestor didn't approve over 3 days).")
                .Set(po => po.UpdatedAt, now)
                .Set(po => po.CompletedAt, now)
                .Set(po => po.Status, PackageOrderStatus.Completed);
            // TODO: chuyển tiền thẳng hết luôn!! ******************************************************************************
            var result = await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(filter, update);
            if (result.ModifiedCount > 0)
            {
                await _stripeService.EscrowReleaseAsync(packageOrderId);
            }

            // có lỗi thì thông báo HERE!
        }

        //NẾU NHƯ SAU DEADLINE MÀ VẪN CHƯA HOÀN THÀNH CÔNG VIỆC THÌ TỰ ĐỘNG ĐƯA VAOF DANH SÁCH CHO MOD XỬ LÝ
        public async Task SolveOverdueAutomatically(string id, DateTimeOffset deadline)
        {
            var now = HelperMethod.GetUtcPlus7TimeOffset();
            if (deadline > now) return;
            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                Builders<PackageOrder>.Filter.Eq(po => po.Id, id)
                );

            var update = Builders<PackageOrder>.Update
                                                .Set(po => po.Status, PackageOrderStatus.Disputed)
                                                .Set(po => po.UpdatedAt, now)
                                                .Set(po => po.DisputedAt, now);
            await _unitOfWork.GetCollection<PackageOrder>().UpdateOneAsync(filter, update);
        }

        //JOB THÔNG BÁO CHO ARTIST KHI SẮP HẾT HẠN
        public async Task NotifyArtistBeforeDeadlineAsync()
        {
            var now = HelperMethod.GetUtcPlus7TimeOffset();

            var filter = Builders<PackageOrder>.Filter.And(
                Builders<PackageOrder>.Filter.Eq(po => po.Status, PackageOrderStatus.InProgress),
                Builders<PackageOrder>.Filter.Where(po => po.StartedAt != null
                                                && (po.StartedAt.Value.AddDays(po.Duration - 1) + po.FreezedTime) <= now
                                                && (po.StartedAt.Value.AddDays(po.Duration) + po.FreezedTime) > HelperMethod.GetUtcPlus7TimeOffset())
                );
            var packageOrders = await _unitOfWork.GetCollection<PackageOrder>().Find(filter).ToListAsync();

            var clientIds = packageOrders.Select(o => o.ClientId).Distinct().ToList();

            var clients = await _unitOfWork.GetCollection<User>()
                .Find(u => clientIds.Contains(u.Id))
                .ToListAsync();

            var clientDict = clients.ToDictionary(c => c.Id, c => c.FullName);

            foreach (var packageOrder in packageOrders)
            {
                string clientName = clientDict.TryGetValue(packageOrder.ClientId, out var name) ? name : "Unknown";
                string content = HelperMethod.BuildContentNotification(NotificationActionType.OrderDeadline, NotificationRelatedType.Order, packageOrder.Id, clientName);

                await _unitOfWork.GetCollection<Notification>()
                    .InsertOneAsync(new Notification
                    {
                        ActorId = packageOrder.ClientId,
                        TargetId = packageOrder.ProviderId,
                        Content = content,
                        Action = NotificationActionType.OrderDeadline,
                        RelatedId = packageOrder.Id,
                        RelatedType = NotificationRelatedType.Track,
                        Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/order/{packageOrder.Id}"
                    });

                await _hubContext.Clients.User(packageOrder.ProviderId).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = content,
                    Avatar = string.Empty,
                });
            }
        }
        #endregion
    }
}
