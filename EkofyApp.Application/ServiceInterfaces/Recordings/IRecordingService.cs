using EkofyApp.Application.Models.Recordings;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Recordings;
public interface IRecordingService
{
    RecordingTempRequest CreateRecordingTemp(CreateRecordingRequest createRecordingRequest);
    IQueryable<Recording> GetRecordingsQueryable();
}
