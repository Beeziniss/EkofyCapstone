using EkofyApp.Application.Models.Requests;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Requests;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Requests
{
    public class RequestService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IRequestService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
                                            .Find(r => r.Id == request.PackageId).Limit(1)
                                            .Project<ArtistPackage>(Builders<ArtistPackage>.Projection
                                                .Include(ap => ap.ArtistId))
                                            .FirstOrDefaultAsync()
                                ?? throw new BadRequestCustomException("Package not found!"); ;


            var publicRequest = await _unitOfWork.GetCollection<Request>()
                                            .Find(r => r.Id == request.PublicRequestId).Limit(1)
                                            .Project<Request>(Builders<Request>.Projection
                                                .Include(r => r.DetailDescription))
                                            .FirstOrDefaultAsync()
                                ?? throw new BadRequestCustomException("Public Request not found!"); ;


            //Nếu là direct request thif tạo mới 
            if (isDirectRequest)
            {
                Request directRequest = new()
                {
                    RequestUserId = userId,
                    ArtistId = request.ArtistId,
                    Budget = request.Budget,
                    Deadline = request.Deadline,
                    Requirements = request.Requirements,
                    Status = RequestStatus.Pending,
                    Type = RequestType.DirectRequest,
                    PackageId = request.PackageId,
                    RequestCreatedTime = HelperMethod.GetUtcPlus7TimeOffset(),
                };

                await _unitOfWork.GetCollection<Request>().InsertOneAsync(directRequest);
                return true;
            }

            //nếu public request thì đổi status của request đã có sẵn và chờ artist duyệt
            var update = Builders<Request>.Update.Set(r => r.Status, RequestStatus.Pending)
                                                 .Set(r => r.PackageId, request.PackageId)
                                                 .Set(r => r.Requirements, publicRequest.DetailDescription)
                                                 .Set(r => r.ArtistId, artistPackage.ArtistId)
                                                 .Set(r => r.RequestCreatedTime, HelperMethod.GetUtcPlus7TimeOffset());
            var result = await _unitOfWork.GetCollection<Request>().UpdateOneAsync(r => r.Id == request.PublicRequestId, update);
            return result.ModifiedCount > 0;
        }

        #region custom direct request

        public async Task<bool> ChangeRequestStatus(ChangeStatusRequest request)
        {
            bool isExist = await _unitOfWork.GetCollection<Request>().Find(r => r.Id == request.RequestId && r.Status == RequestStatus.Pending).AnyAsync();

            if (!isExist)
            {
                throw new NotFoundCustomException("Not find suitable request to update!");
            }

            // chỉ cho update trong các status này
            if(request.Status != RequestStatus.Canceled || request.Status != RequestStatus.Confirmed || request.Status != RequestStatus.Rejected)
            {
                throw new BadRequestCustomException("Invalid status update!");
            }

            var update = Builders<Request>.Update.Set(r => r.Status, request.Status)
                                                 .Set(r => r.Notes,  "The request is " + request.Status.ToString().ToLower() + " by the artist");
            var result = await _unitOfWork.GetCollection<Request>().UpdateOneAsync(r => r.Id == request.RequestId, update);

            return result.ModifiedCount > 0;
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
                Deadline = HelperMethod.ConvertDateTimeToUtcPlus7TimeOffset(request.Deadline),
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
                                                        .Include(rh => rh.Deadline))
                                                     .FirstOrDefaultAsync();

            //check xem bài request này có đúng là của người đang muốn sửa ko
            if (requestHub.RequestUserId != userId)
            {
                throw new ForbiddenCustomException("You do not have permission to edit request!");
            }

            List<UpdateDefinition<Request>> updatedFields = new();

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
            if (request.Deadline.HasValue)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Deadline, HelperMethod.ConvertDateTimeToUtcPlus7TimeOffset(request.Deadline.Value)));
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
        public async Task AutoCloseExpiredRequestsAsync()
        {
            var filter = Builders<Request>.Filter.And(
                Builders<Request>.Filter.Lte(rh => rh.RequestCreatedTime, HelperMethod.GetUtcPlus7TimeOffset().AddDays(-3)),
                Builders<Request>.Filter.Eq(rh => rh.Status, RequestStatus.Pending)
            );
            var update = Builders<Request>.Update.Set(rh => rh.Status, RequestStatus.Rejected)
                                                 .Set(rh => rh.Notes, "The request is reject by the system because of overdue!");


            //GỬI THÔNG BÁO ĐÂY

            await _unitOfWork.GetCollection<Request>().UpdateManyAsync(filter, update);
        }
    }
}
