using EkofyApp.Application.Models.Artists;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Artists;
public interface IArtistService
{
    Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest);
    IQueryable<Artist> GetArtistsQueryable();
    Task UpdateArtistAsync(UpdateArtistRequest updateArtistRequest);
    
    // Artist Registration Approval Methods
    Task<IEnumerable<PendingArtistRegistrationResponse>> GetPendingRegistrationsAsync(int pageNumber = 1, int pageSize = 20);
    Task ApproveArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest);
    Task RejectArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest);
}
