using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;
public sealed record EscrowPaymentResponse
{
    public string Id { get; init; } = null!;
    public string OrderId { get; init; } = null!;
    public string PaymentTransactionId { get; init; } = null!;
    public string StripePaymentIntentId { get; init; } = null!;
    
    public decimal TotalAmount { get; init; }
    public decimal AdvancePaymentAmount { get; init; }
    public decimal CompletionPaymentAmount { get; init; }
    public decimal PlatformCommissionAmount { get; init; }
    public string Currency { get; init; } = null!;
    
    public EscrowTransactionStatus Status { get; init; }
    public ArtistPackageOrderStatus OrderStatus { get; init; }
    
    public DateTimeOffset? AdvancePaymentReleasedAt { get; init; }
    public DateTimeOffset? OrderCompletedAt { get; init; }
    public DateTimeOffset? AutoReleaseDate { get; init; }
    
    public string BuyerId { get; init; } = null!;
    public string ArtistId { get; init; } = null!;
    public string ArtistPackageId { get; init; } = null!;
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}