namespace EkofyApp.Application.Models.Artists;
public sealed record class ArtistRevenueResponse
{
    public decimal RoyaltyEarnings { get; init; }
    public decimal ServiceRevenue { get; init; } // Tiền chưa trừ hoa hồng
    public decimal GrossRevenue => RoyaltyEarnings + ServiceRevenue;
    public decimal RefundAmount { get; init; }
}
