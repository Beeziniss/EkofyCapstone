using EkofyApp.Application.Models.Recordings;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Recordings;
public interface IRecordingService
{
    Task CreateRecordingAsync(CreateRecordingRequest createRecordingRequest, string trackId, CancellationToken cancellationToken = default);
    RecordingTempRequest CreateRecordingTemp(CreateRecordingRequest createRecordingRequest);
    IQueryable<Recording> GetRecordings();
}
