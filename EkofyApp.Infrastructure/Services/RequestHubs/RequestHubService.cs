using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Domain.Entities;
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

        public async Task<bool> CreateRequestAsync(RequestCreatingRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            // tạo request từ data của model
            RequestHub requestHub = new()
            {
                RequestUserId = userId,
                Title = request.Title,
                Summary = request.Summary,
                DetailDescription = request.DetailDescription,
                Deadline = request.Deadline,
                Budget = request.Budget
            };
            // lưu request vừa mới tạo
            await _unitOfWork.GetCollection<RequestHub>().InsertOneAsync(requestHub);
            return true;
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            RequestHub requestHub = await _unitOfWork.GetCollection<RequestHub>()
                                                     .Find(rh => rh.Id == request.Id)
                                                     .Project<RequestHub>(Builders<RequestHub>.Projection
                                                        .Include(rh => rh.IsClosed)
                                                        .Include(rh => rh.IsDeleted)
                                                        .Include(rh => rh.RequestUserId))
                                                     .FirstOrDefaultAsync();

            //kiểm tra request có hợp lệ hay không, ko thì ném lỗi
            if (requestHub.IsClosed || requestHub.IsDeleted)
            {
                throw new BadRequestCustomException("The request has been closed or deleted, cannot update anymoore!");
            }

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
            }
            if (request.Summary != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Summary, request.Summary));
            }
            if (request.DetailDescription != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.DetailDescription, request.DetailDescription));
            }
            if (request.Deadline != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Deadline, request.Deadline));
            }
            if (request.Budget != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Budget, request.Budget));
            }

            //CHỖ NÀY PHẢI TỚI KHI TẠO ORDER REQUEST MỚI ĐÓNG LẠI (ĐÓNG -> KO CHO SỬA NỮA)
            //if (request.IsClosed != null)
            //{
            //    updatedFields.Add(updateBuilder.Set(rh => rh.IsClosed, request.IsClosed));
            //}
            if (request.IsDeleted != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.IsDeleted, request.IsDeleted));
            }

            //cập nhật lại thời gian sửa đổi
            updatedFields.Add(updateBuilder.Set(rh => rh.UpdatedAt, HelperMethod.GetUtcPlus7TimeOffset()));

            //gộp các field đã được cập nhật
            var updateBuilderCombine = updateBuilder.Combine(updatedFields);

            var result = await _unitOfWork.GetCollection<RequestHub>()
                             .UpdateOneAsync(rh => rh.Id == request.Id, updateBuilderCombine);

            return result.ModifiedCount > 0;
        }

    }
}
