using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;
public sealed record CreateEscrowPaymentRequest
{
    public string ArtistPackageId { get; init; } = null!;
    public string OrderDescription { get; init; } = null!;
    public List<string> RequirementFiles { get; init; } = [];
    public Dictionary<string, string> CustomRequirements { get; init; } = [];
    
    // Payment URLs
    public string SuccessUrl { get; init; } = null!;
    public string CancelUrl { get; init; } = null!;
    
    // Optional customization
    public decimal? AdvancePaymentPercentage { get; init; } // Default 30%
    public decimal? CompletionPaymentPercentage { get; init; } // Default 60%
    public decimal? PlatformCommissionPercentage { get; init; } // Default 10%
    
    public int EstimatedDeliveryDays { get; init; } = 7; // Default 7 days
    public int MaxRevisions { get; init; } = 3;
    
    public bool IsSavePaymentMethod { get; init; } = false;
    public bool IsReceiptEmail { get; init; } = false;
}