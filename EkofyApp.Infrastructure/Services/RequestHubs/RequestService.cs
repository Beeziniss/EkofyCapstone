using EkofyApp.Application.Models.RequestHub;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.RequestHubs;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.RequestHubs
{
    public sealed class RequestService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IRequestHubService
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

        public async Task<bool> UpdateRequestAsync(string id, RequestUpdatingRequest request)
        {
            string artistId = _httpContextAccessor.HttpContext?.User.FindFirst("userId")?.Value ?? throw new UnauthorizedCustomException("Your session is limit");

            RequestHub requestHub = await _unitOfWork.GetCollection<RequestHub>()
                                                     .Find(rh => rh.Id == id)
                                                     .FirstOrDefaultAsync();

            //kiểm tra request có hợp lệ hay không
            if (requestHub.IsClosed || requestHub.IsDeleted)
            {
                throw new BadRequestCustomException("The request has been closed or deleted, cannot update anymoore!");
            }

            //nếu input chưa có giá trị thì lấy giá trị cũ
            request.Title ??= requestHub.Title;
            request.Description ??= requestHub.Description;
            request.Attachments ??= requestHub.Attachments;
            request.IsDeleted ??= requestHub.IsDeleted;
            request.IsClosed ??= requestHub.IsClosed;

            requestHub.Title = request.Title;
            requestHub.Description = request.Description;
            requestHub.Attachments = request.Attachments;
            requestHub.IsClosed = (bool) request.IsClosed;
            requestHub.IsDeleted = (bool) request.IsDeleted;

            //update request
            await _unitOfWork.GetCollection<RequestHub>()
                             .ReplaceOneAsync(rh => rh.Id == id, requestHub);

            await _unitOfWork.CommitAsync();

            return true;
        }

        // NOT DO YET
        public async Task SendRequestCommentAsync(CreateRequestCommentRequest commentRequest)
        {
            
        }

    }
}
