using EkofyApp.Application.Models.RequestHub;
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

        public IQueryable<Request> GetOwnRequestsAsync()
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            // Không cho xem lại các request đã bị blocked hay đã xóa
            return _unitOfWork.GetCollection<Request>()
                                                      .AsQueryable()
                                                      .Where(rh => rh.RequestUserId == userId && (
                                                             rh.Status == RequestStatus.Open ||
                                                             rh.Status == RequestStatus.Closed
                                                      ));
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
                (t.Title.Contains(unsignedSearchTerm) || t.Summary.Contains(unsignedSearchTerm)) &&
                t.Status == RequestStatus.Open);
        }

        public async Task<bool> CreateRequestAsync(RequestCreatingRequest request)
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
                Status = RequestStatus.Open
            };
            // lưu request vừa mới tạo
            await _unitOfWork.GetCollection<Request>().InsertOneAsync(requestHub);
            return true;
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            Request requestHub = await _unitOfWork.GetCollection<Request>()
                                                     .Find(rh => rh.Id == request.Id && rh.Status == RequestStatus.Open)
                                                     .Project<Request>(Builders<Request>.Projection
                                                        .Include(rh => rh.RequestUserId)
                                                        .Include(rh => rh.Deadline))
                                                     .FirstOrDefaultAsync();

            //check xem bài request này có đúng là của người đang muốn sửa ko
            if(requestHub.RequestUserId != userId)
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

        public async Task<bool> BlockRequestAsync(string requestId)
        {
            var update = Builders<Request>.Update
                                             .Set(rh => rh.Status, RequestStatus.Blocked)
                                             .Set(rh => rh.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());
            var result = await _unitOfWork.GetCollection<Request>()
                             .UpdateOneAsync(rh => rh.Id == requestId, update);
            return result.ModifiedCount > 0;
        }
    }
}
