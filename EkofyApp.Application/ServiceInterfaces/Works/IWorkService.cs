using EkofyApp.Application.Models.Works;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Works;
public interface IWorkService
{
    WorkTempRequest CreateWorkTemp(CreateWorkRequest createWorkRequest);
    IQueryable<Work> GetWorksQueryable();
}
