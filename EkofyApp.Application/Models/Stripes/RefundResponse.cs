using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;
public sealed record RefundResponse
{
    public string Id { get; init; } = null!;
    public string StripeRefundId { get; init; } = null!;
    public string PaymentTransactionId { get; init; } = null!;
    public string StripePaymentIntentId { get; init; } = null!;
    public string? StripeChargeId { get; init; }
    
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    
    public RefundType Type { get; init; }
    public RefundTransactionStatus Status { get; init; }
    
    public string? Reason { get; init; }
    public string? Description { get; init; }
    
    public Dictionary<string, string>? Metadata { get; init; }
    
    public string? ProcessedByUserId { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}