using EkofyApp.Application.Models.Recordings;
using EkofyApp.Application.ServiceInterfaces;
using EkofyApp.Application.ServiceInterfaces.Recordings;
using EkofyApp.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EkofyApp.Infrastructure.Services.Recordings;
public sealed class RecordingService(IUnitOfWork unitOfWork) : IRecordingService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public IQueryable<Recording> GetRecordingsQueryable()
    {
        return _unitOfWork.GetCollection<Recording>().AsQueryable();
    }

    public RecordingTempRequest CreateRecordingTemp(CreateRecordingRequest createRecordingRequest)
    {
        return new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Description = createRecordingRequest.Description,
            RecordingSplitRequests = createRecordingRequest.RecordingSplits,
        };
    }
}
