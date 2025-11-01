using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.RequestHubs
{
    public class RequestHubService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IRequestHubService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public IQueryable<RequestHub> GetRequestsQueryable()
        {
            return _unitOfWork.GetCollection<RequestHub>().AsQueryable();
        }

        public async Task<RequestHub?> GetRequestByIdAsync(string requestId)
        {
            return await _unitOfWork.GetCollection<RequestHub>()
                                    .Find(rh => rh.Id == requestId && rh.Status == RequestStatus.Open)
                                    .FirstOrDefaultAsync();
        }

        public IQueryable<RequestHub> GetOwnRequestsAsync()
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");
            // Không cho xem lại các request đã bị blocked hay đã xóa
            return _unitOfWork.GetCollection<RequestHub>()
                                                      .AsQueryable()
                                                      .Where(rh => rh.RequestUserId == userId && (
                                                             rh.Status == RequestStatus.Open ||
                                                             rh.Status == RequestStatus.Closed
                                                      ));
        }

        public IQueryable<RequestHub> SearchRequests(string searchTerm, bool isIndividual)
        {
            var query = _unitOfWork.GetCollection<RequestHub>().AsQueryable();
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
            RequestHub requestHub = new()
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
            await _unitOfWork.GetCollection<RequestHub>().InsertOneAsync(requestHub);
            return true;
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            RequestHub requestHub = await _unitOfWork.GetCollection<RequestHub>()
                                                     .Find(rh => rh.Id == request.Id && rh.Status == RequestStatus.Open)
                                                     .Project<RequestHub>(Builders<RequestHub>.Projection
                                                        .Include(rh => rh.RequestUserId)
                                                        .Include(rh => rh.Deadline))
                                                     .FirstOrDefaultAsync();

            //check xem bài request này có đúng là của người đang muốn sửa ko
            if(requestHub.RequestUserId != userId)
            {
                throw new ForbiddenCustomException("You do not have permission to edit request!");
            }

            List<UpdateDefinition<RequestHub>> updatedFields = new();

            UpdateDefinitionBuilder<RequestHub> updateBuilder = Builders<RequestHub>.Update;

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

            var result = await _unitOfWork.GetCollection<RequestHub>()
                             .UpdateOneAsync(rh => rh.Id == request.Id, updateBuilderCombine);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> BlockRequestAsync(string requestId)
        {
            var update = Builders<RequestHub>.Update
                                             .Set(rh => rh.Status, RequestStatus.Blocked)
                                             .Set(rh => rh.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset());
            var result = await _unitOfWork.GetCollection<RequestHub>()
                             .UpdateOneAsync(rh => rh.Id == requestId, update);
            return result.ModifiedCount > 0;
        }
    }
}
