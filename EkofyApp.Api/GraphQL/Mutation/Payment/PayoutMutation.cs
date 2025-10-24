using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Payment;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class PayoutMutation(IRoyaltyReportService royaltyReportService)
{
    private readonly IRoyaltyReportService _royaltyReportService = royaltyReportService;

    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    public async Task<bool> ProcessPayoutForArtistAsync(string artistId, decimal amount, bool isInstant = false)
    {
        return await _royaltyReportService.ProcessPayoutForArtistAsync(artistId, amount, isInstant);
    }

    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    public async Task<bool> ProcessPayoutsForAllArtistsAsync(int month, int year, bool isInstant = false)
    {
        return await _royaltyReportService.ProcessPayoutsForAllArtistsAsync(month, year, isInstant);
    }
}