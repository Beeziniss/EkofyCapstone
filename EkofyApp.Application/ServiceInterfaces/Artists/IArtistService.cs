using EkofyApp.Application.Models.Artists;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Redis;
using EkofyApp.Domain.Entities;

namespace EkofyApp.Application.ServiceInterfaces.Artists;
public interface IArtistService
{
    Task<bool> CreateArtistAsync(CreateArtistRequest createArtistRequest);
    IQueryable<Artist> GetArtistsQueryable();
    Task UpdateProfileAsync(UpdateArtistRequest updateArtistRequest);
    
    // Artist Registration Approval Methods
    Task<PaginatedData<PendingArtistRegistrationResponse>> GetPendingRegistrationsAsync(int pageNumber = 1, int pageSize = 20);
    Task<PendingArtistRegistrationResponse> GetPendingRegistrationByIdAsync(string artistRegistrationId);
    Task ApproveArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest);
    Task RejectArtistRegistrationAsync(ArtistRegistrationApprovalRequest approvalRequest);
    IQueryable<Artist> SearchArtists(string stageName);
    Task<string> GetArtistStageNameByArtistIdAsync(string artistId);
}
