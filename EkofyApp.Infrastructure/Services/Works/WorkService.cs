using EkofyApp.Application.Models.Works;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Works;
using EkofyApp.Domain.Entities;
using EkofyApp.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Works;
public sealed class WorkService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : IWorkService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IQueryable<Work> GetWorksQueryable()
    {
        return _unitOfWork.GetCollection<Work>().AsQueryable();
    }

    public WorkTempRequest CreateWorkTemp(CreateWorkRequest createWorkRequest)
    {
        return new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Description = createWorkRequest.Description,
            WorkSplits = createWorkRequest.WorkSplits,
        };
    }
}
