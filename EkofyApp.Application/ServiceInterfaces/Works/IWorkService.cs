using EkofyApp.Application.Models.Works;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Works;
public interface IWorkService
{
    Task CreateWorkAsync(CreateWorkRequest createWorkRequest, string trackId, CancellationToken cancellationToken = default);
    WorkTempRequest CreateWorkTemp(CreateWorkRequest createWorkRequest);
    IQueryable<Work> GetWorksQueryable();
}
