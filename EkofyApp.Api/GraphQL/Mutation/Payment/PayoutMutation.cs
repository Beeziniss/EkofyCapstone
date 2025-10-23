using EkofyApp.Application.ServiceInterfaces.RoyaltyReports;
using EkofyApp.Domain.Utils;

namespace EkofyApp.Api.GraphQL.Mutation.Payment;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public sealed class PayoutMutation(IRoyaltyReportService royaltyReportService)
{
    private readonly IRoyaltyReportService _royaltyReportService = royaltyReportService;

    /// <summary>
    /// Manual payout cho một artist cụ thể (Admin only)
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    public async Task<bool> ProcessPayoutForArtistAsync(string artistId, decimal amount, bool isInstant = false)
    {
        return await _royaltyReportService.ProcessPayoutForArtistAsync(artistId, amount, isInstant);
    }

    /// <summary>
    /// Batch payout cho tất cả artists có pending royalty trong tháng (Admin only)
    /// </summary>
    [AuthorizeRoles(HelperRoleBase.AdminRoles)]
    public async Task<bool> ProcessPayoutsForAllArtistsAsync(int month, int year, bool isInstant = false)
    {
        return await _royaltyReportService.ProcessPayoutsForAllArtistsAsync(month, year, isInstant);
    }
}