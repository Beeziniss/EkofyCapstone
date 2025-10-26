using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Stripes;
public sealed record CreateRefundRequest
{
    public string PaymentTransactionId { get; init; } = null!;
    public decimal? Amount { get; init; } // Null = full refund, có giá trị = partial refund
    public string? Reason { get; init; } // duplicate, fraudulent, requested_by_customer
    public string? Description { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public bool ReverseTransfer { get; init; } = false; // Có reverse transfer không (nếu có connect account)
}