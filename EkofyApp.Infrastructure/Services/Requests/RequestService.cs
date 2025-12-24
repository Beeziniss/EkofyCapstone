using EkofyApp.Application.Models.Notifications;
using EkofyApp.Application.Models.Requests;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Chat;
using EkofyApp.Application.ServiceInterfaces.Notifications;
using EkofyApp.Application.ServiceInterfaces.Requests;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using EkofyApp.Infrastructure.Services.Notifications;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Requests
{
    [Queue("request")]
    public class RequestService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, INotificationService notificationService, IHubContext<NotificationHub> hubContext) : IRequestService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        public IQueryable<Request> GetRequestsQueryable()
        {
            return _unitOfWork.GetCollection<Request>().AsQueryable();
        }

        public async Task<Request?> GetRequestByIdAsync(string requestId)
        {
            return await _unitOfWork.GetCollection<Request>()
                                    .Find(rh => rh.Id == requestId && rh.Status == RequestStatus.Open)
                                    .FirstOrDefaultAsync();
        }

        public async Task<bool> SendRequest(CreateDirectRequest request, bool isDirectRequest)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            var artistPackage = await _unitOfWork.GetCollection<ArtistPackage>()
                                            .Find(r => r.Id == request.PackageId)
                                            .Limit(1)
                                            .Project<ArtistPackage>(Builders<ArtistPackage>.Projection
                                                .Include(ap => ap.ArtistId)
                                                .Include(ap => ap.EstimateDeliveryDays))
                                            .FirstOrDefaultAsync()
                                ?? throw new BadRequestCustomException("Package not found!"); ;

            string displayName = string.Empty;
            string userArtistId = string.Empty;
            string content = string.Empty;

            //Nếu là direct request thif tạo mới 
            if (isDirectRequest)
            {
                Request directRequest = new()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    RequestUserId = userId,
                    ArtistId = request.ArtistId,
                    Duration = artistPackage.EstimateDeliveryDays,
                    Requirements = request.Requirements,
                    Status = RequestStatus.Pending,
                    Type = RequestType.DirectRequest,
                    PackageId = request.PackageId,
                    RequestCreatedTime = HelperMethod.GetUtcPlus7TimeOffset(),
                };

                await _unitOfWork.GetCollection<Request>().InsertOneAsync(directRequest);

                var expiredTimeDirectRequest = HelperMethod.GetUtcPlus7TimeOffset().AddDays(3);

                BackgroundJob.Schedule<RequestService>(service => service.AutoCloseExpiredRequestsAsync(directRequest.Id), expiredTimeDirectRequest);

                displayName = await _unitOfWork.GetCollection<Listener>()
                .Find(x => x.UserId == userId)
                .Project(x => x.DisplayName)
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Listener not found");

                userArtistId = await _unitOfWork.GetCollection<Artist>()
                    .Find(x => x.Id == artistPackage.ArtistId)
                    .Project(x => x.UserId)
                    .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");

                content = HelperMethod.BuildContentNotification(
                    NotificationActionType.RequestCreated,
                    NotificationRelatedType.Request,
                    null,
                    displayName
                );

                await _hubContext.Clients.User(userArtistId).SendAsync("ReceiveNotification", new NotificationResponse
                {
                    Content = content,
                });

                await _unitOfWork.GetCollection<Notification>().InsertOneAsync(new Notification
                {
                    ActorId = userId,
                    TargetId = userArtistId,
                    Content = content,
                    RelatedType = NotificationRelatedType.Request,
                    RelatedId = directRequest.Id,
                    Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/artist/studio/pending-request/{directRequest.Id}",
                });

                return true;
            }

            var publicRequest = await _unitOfWork.GetCollection<Request>()
                                            .Find(r => r.Id == request.PublicRequestId).Limit(1)
                                            .Project<Request>(Builders<Request>.Projection
                                                .Include(r => r.DetailDescription)
                                                .Include(r => r.Id))
                                            .FirstOrDefaultAsync()
                                ?? throw new BadRequestCustomException("Public Request not found!");

            //nếu public request thì đổi status của request đã có sẵn và chờ artist duyệt
            var update = Builders<Request>.Update.Set(r => r.Status, RequestStatus.Pending)
                                                 .Set(r => r.PackageId, request.PackageId)
                                                 .Set(r => r.Requirements, publicRequest.DetailDescription)
                                                 .Set(r => r.ArtistId, artistPackage.ArtistId)
                                                 .Set(r => r.RequestCreatedTime, HelperMethod.GetUtcPlus7TimeOffset());
            var result = await _unitOfWork.GetCollection<Request>().UpdateOneAsync(r => r.Id == request.PublicRequestId, update);
            if(result.ModifiedCount <= 0)
            {
                return false;
            }

            var expiredTime = HelperMethod.GetUtcPlus7TimeOffset().AddDays(3);

            BackgroundJob.Schedule<RequestService>(service => service.AutoCloseExpiredRequestsAsync(publicRequest.Id), expiredTime);

            displayName = await _unitOfWork.GetCollection<Listener>()
                .Find(x => x.UserId == userId)
                .Project(x => x.DisplayName)
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Listener not found");

            userArtistId = await _unitOfWork.GetCollection<Artist>()
                .Find(x => x.Id == artistPackage.ArtistId)
                .Project(x => x.UserId)
                .FirstOrDefaultAsync() ?? throw new NotFoundCustomException("Artist not found");

            content = HelperMethod.BuildContentNotification(
                NotificationActionType.RequestCreated,
                NotificationRelatedType.Request,
                null,
                displayName
            );

            await _hubContext.Clients.User(userArtistId).SendAsync("ReceiveNotification", new NotificationResponse
            {
                Content = content,
            });

            await _unitOfWork.GetCollection<Notification>().InsertOneAsync(new Notification
            {
                ActorId = userId,
                TargetId = userArtistId,
                Content = content,
                RelatedType = NotificationRelatedType.Request,
                RelatedId = request.PublicRequestId,
                Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/artist/studio/pending-request/{request.PublicRequestId}",
            });

            return true;
        }

        #region custom direct request

        public async Task<bool> ChangeRequestStatus(ChangeStatusRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            var requestDocument = await _unitOfWork.GetCollection<Request>()
                                                    .Find(r => r.Id == request.RequestId && r.Status == RequestStatus.Pending)
                                                    .Project<Request>(Builders<Request>.Projection
                                                        .Include(r => r.Status)
                                                        .Include(r => r.RequestUserId)
                                                        .Include(r => r.Type))
                                                    .FirstOrDefaultAsync()
                                  ?? throw new NotFoundCustomException("Not find suitable request to update!");

            // chỉ cho update trong các status này
            if(request.Status != RequestStatus.Canceled && request.Status != RequestStatus.Confirmed && request.Status != RequestStatus.Rejected)
            {
                throw new BadRequestCustomException("Invalid status update!");
            }

            //Sau khi artist xác nhận thì mới đóng conversation của bên khác
            //if (request.Status == RequestStatus.Confirmed && requestDocument.Type == RequestType.PublicRequest)
            //{
            //    var conversations = _unitOfWork.GetCollection<Conversation>();

            //    // Update cho user artist xác nhận
            //    var updateUserConversation = conversations.UpdateOneAsync(c => c.RequestId == request.RequestId && c.UserIds.Contains(userId), Builders<Conversation>.Update.Set(c => c.Status, ConversationStatus.Confirmed));

            //    // Update cho các user còn lại
            //    var updateOtherUsersConversation = conversations.UpdateManyAsync(c => c.RequestId == request.RequestId && !c.UserIds.Contains(userId), Builders<Conversation>.Update.Set(c => c.Status, ConversationStatus.Cancelled));

            //    await Task.WhenAll(updateUserConversation, updateOtherUsersConversation);

            //    if (updateUserConversation.Result.ModifiedCount <= 0)
            //    {
            //        throw new BadRequestCustomException(
            //            "Cannot update conversation after confirming request!"
            //        );
            //    }
            //}
            //if (request.Status == RequestStatus.Confirmed && requestDocument.Type == RequestType.DirectRequest)
            //{
            //    await _chatService.AddConversationFromRequestAsync(new()
            //    {
            //        OtherUserId = requestDocument.RequestUserId,
            //        RequestId = request.RequestId
            //    });
            //}

            var update = Builders<Request>.Update.Set(r => r.Status, request.Status)
                                                 .Set(r => r.Notes,  "The request is " + request.Status.ToString().ToLower() + " by the artist");

            var result = await _unitOfWork.GetCollection<Request>().UpdateOneAsync(r => r.Id == request.RequestId, update);
            if(result.ModifiedCount <= 0)
            {
                return false;
            }

            string content = HelperMethod.BuildContentNotification(
                request.Status == RequestStatus.Confirmed ? NotificationActionType.RequestApproved : NotificationActionType.RequestRejected,
                NotificationRelatedType.Request,
                null,
                string.Empty
            );

            await _hubContext.Clients.User(requestDocument.RequestUserId).SendAsync("ReceiveNotification", new NotificationResponse
            {
                Content = content,
            });

            await _unitOfWork.GetCollection<Notification>().InsertOneAsync(new Notification
            {
                ActorId = userId,
                TargetId = requestDocument.RequestUserId,
                Content = content,
                RelatedType = NotificationRelatedType.Request,
                RelatedId = request.RequestId,
                Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/profile/my-requests/{request.RequestId}",
            });

            Dictionary<string, string> data = [];
            data.Add("mobileRoute", "/own-requests");

            string status = request.Status == RequestStatus.Confirmed ? "approved" : "rejected";

            await _notificationService.SendFcmNotificationAsync(requestDocument.RequestUserId, "Request Change", $"Your request has been {status}.", "request", data);

            return true;
        }
        #endregion

        #region Request Hub
        public IQueryable<Request> GetOwnRequestsAsync()
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            // Không cho xem lại các request đã bị blocked hay đã xóa
            return _unitOfWork.GetCollection<Request>()
                                                      .AsQueryable()
                                                      .Where(rh => rh.RequestUserId == userId && 
                                                            rh.Type == RequestType.PublicRequest &&
                                                            (
                                                             rh.Status == RequestStatus.Open ||
                                                             rh.Status == RequestStatus.Closed
                                                            )
                                                      );
        }

        public IQueryable<Request> SearchRequests(string searchTerm, bool isIndividual)
        {
            var query = _unitOfWork.GetCollection<Request>().AsQueryable();
            string unsignedSearchTerm = HelperMethod.ToUnsigned(searchTerm);

            //search cá nhân
            if (isIndividual)
            {
                string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value
                    ?? throw new UnauthorizedCustomException("Your session is limit");

                query = query.Where(rh => rh.RequestUserId == userId);
            }

            // Ko có search term thì trả về luôn query hiện tại
            if (string.IsNullOrEmpty(searchTerm))
                return query;

            // có search term thì lọc rồi return
            return query.Where(t =>
                (t.Title!.Contains(unsignedSearchTerm) || t.Summary!.Contains(unsignedSearchTerm)) &&
                t.Status == RequestStatus.Open && t.Type == RequestType.PublicRequest);
        }

        public async Task<bool> CreatePublicRequestAsync(RequestCreatingRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            // tạo request từ data của model
            Request requestHub = new()
            {
                RequestUserId = userId,
                Title = request.Title,
                TitleUnsigned = HelperMethod.ToUnsigned(request.Title),
                Summary = request.Summary,
                SummaryUnsigned = HelperMethod.ToUnsigned(request.Summary),
                DetailDescription = request.DetailDescription,
                Duration = request.Duration,
                Budget = request.Budget,
                Status = RequestStatus.Open,
                Type = RequestType.PublicRequest,
                PostCreatedTime = HelperMethod.GetUtcPlus7TimeOffset(),
            };
            // lưu request vừa mới tạo
            await _unitOfWork.GetCollection<Request>().InsertOneAsync(requestHub);
            return true;
        }

        public async Task<bool> UpdatePublicRequestAsync(RequestUpdatingRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            Request requestHub = await _unitOfWork.GetCollection<Request>()
                                                     .Find(rh => rh.Id == request.Id && rh.Status == RequestStatus.Open && rh.Type == RequestType.PublicRequest)
                                                     .Project<Request>(Builders<Request>.Projection
                                                        .Include(rh => rh.RequestUserId)
                                                        .Include(rh => rh.Duration))
                                                     .FirstOrDefaultAsync()
                                 ?? throw new BadRequestCustomException("Invalid to update this request!");

            //check xem bài request này có đúng là của người đang muốn sửa ko
            if (requestHub.RequestUserId != userId)
            {
                throw new ForbiddenCustomException("You do not have permission to edit request!");
            }
            if (request.Status is not null && request.Status != RequestStatus.Deleted)
            {
                throw new BadRequestCustomException("Invalid status update!");
            }

            List<UpdateDefinition<Request>> updatedFields = [];

            UpdateDefinitionBuilder<Request> updateBuilder = Builders<Request>.Update;

            if (request.Title != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Title, request.Title));
                updatedFields.Add(updateBuilder.Set(rh => rh.TitleUnsigned, HelperMethod.ToUnsigned(request.Title)));
            }
            if (request.Summary != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Summary, request.Summary));
                updatedFields.Add(updateBuilder.Set(rh => rh.SummaryUnsigned, HelperMethod.ToUnsigned(request.Summary)));
            }
            if (request.DetailDescription != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.DetailDescription, request.DetailDescription));
            }
            if (request.Duration.HasValue)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Duration, request.Duration.Value));
            }
            if (request.Budget != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Budget, request.Budget));
            }

            // CHỖ NÀY ĐỂ CẬP NHẬT TRẠNG THÁI CỦA REQUEST 
            if (request.Status != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Status, request.Status));
            }

            //cập nhật lại thời gian sửa đổi
            updatedFields.Add(updateBuilder.Set(rh => rh.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));

            //gộp các field đã được cập nhật
            var updateBuilderCombine = updateBuilder.Combine(updatedFields);

            var result = await _unitOfWork.GetCollection<Request>()
                             .UpdateOneAsync(rh => rh.Id == request.Id, updateBuilderCombine);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> BlockPublicRequestAsync(string requestId)
        {
            var update = Builders<Request>.Update
                                             .Set(rh => rh.Status, RequestStatus.Blocked)
                                             .Set(rh => rh.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());
            var result = await _unitOfWork.GetCollection<Request>()
                             .UpdateOneAsync(rh => rh.Id == requestId, update);
            return result.ModifiedCount > 0;
        }
        #endregion


        //TODO: background job for auto requests that expired
        public async Task AutoCloseExpiredRequestsAsync(string requestId)
        {
            var request = await _unitOfWork.GetCollection<Request>().Find(rh => rh.Id == requestId).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("The request not found when auto close expire!");

            var filter = Builders<Request>.Filter.And(
                Builders<Request>.Filter.Eq(rh => rh.Id, requestId),
                Builders<Request>.Filter.Eq(rh => rh.Status, RequestStatus.Pending)
            );
            var update = Builders<Request>.Update.Set(rh => rh.Status, RequestStatus.Rejected)
                                                 .Set(rh => rh.Notes, "The request is reject by the system because of overdue!");

            var artist = await _unitOfWork.GetCollection<Artist>().Find(a => a.Id == request.ArtistId).FirstOrDefaultAsync() ?? throw new NotFoundCustomException("The artist not found when auto close expired request");

            //GỬI THÔNG BÁO ĐÂY
            string content = HelperMethod.BuildContentNotification(
                    NotificationActionType.RequestRejected,
                    NotificationRelatedType.Request,
                    null,
                    string.Empty
                    );

            await _hubContext.Clients.User(request.ArtistId!).SendAsync("ReceiveNotification", new NotificationResponse
            {
                Content = content,
            });

            await _unitOfWork.GetCollection<Notification>().InsertOneAsync(new Notification
            {
                ActorId = request.RequestUserId,
                TargetId = artist.UserId,
                Content = content,
                RelatedType = NotificationRelatedType.Request,
                RelatedId = request.Id,
                Url = $"{Environment.GetEnvironmentVariable("FRONTEND_URL")}/artist/studio/pending-request/{requestId}",
            });

            await _unitOfWork.GetCollection<Request>().UpdateManyAsync(filter, update);

            Dictionary<string, string> data = [];
            data.Add("mobileRoute", "/own-requests");

            await _notificationService.SendFcmNotificationAsync(request.RequestUserId, "Request Rejected", "Your request has been rejected because of overdue!", "request", data);
        }
    }
}
