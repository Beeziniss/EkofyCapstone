using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
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
            string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            // tạo request từ data của model
            RequestHub requestHub = new()
            {
                Title = request.Title,
                Description = request.Description,
                Attachments = request.Attachments ?? new List<string>()
            };
            // lưu request vừa mới tạo
            await _unitOfWork.GetCollection<RequestHub>().InsertOneAsync(requestHub);
            return true;
        }

        public async Task<bool> UpdateRequestAsync(RequestUpdatingRequest request)
        {
            string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            RequestHub requestHub = await _unitOfWork.GetCollection<RequestHub>()
                                                     .Find(rh => rh.Id == request.Id)
                                                     .Project<RequestHub>(Builders<RequestHub>.Projection
                                                        .Include(isClose => isClose.IsClosed)
                                                        .Include(isDelete => isDelete.IsDeleted))
                                                     .FirstOrDefaultAsync();

            //kiểm tra request có hợp lệ hay không
            if (requestHub.IsClosed || requestHub.IsDeleted)
            {
                throw new BadRequestCustomException("The request has been closed or deleted, cannot update anymoore!");
            }

            List<UpdateDefinition<RequestHub>> updatedFields = new();

            UpdateDefinitionBuilder<RequestHub> updateBuilder = Builders<RequestHub>.Update;

            if(request.Title != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Title, request.Title));
            }
            if(request.Description != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Description, request.Description));
            }
            if(request.Attachments != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.Attachments, request.Attachments));
            }
            if(request.IsClosed != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.IsClosed, request.IsClosed));
            }
            if(request.IsDeleted != null)
            {
                updatedFields.Add(updateBuilder.Set(rh => rh.IsDeleted, request.IsDeleted));
            }

            var updateBuilderCombine  = updateBuilder.Combine(updatedFields);
            
            var result = await _unitOfWork.GetCollection<RequestHub>()
                             .UpdateOneAsync(rh => rh.Id == request.Id, updateBuilderCombine);

            return result.ModifiedCount > 0;
        }

        // NOT DO YET
        public async Task SendRequestCommentAsync(CreateRequestCommentRequest commentRequest)
        {
            
        }

    }
}
