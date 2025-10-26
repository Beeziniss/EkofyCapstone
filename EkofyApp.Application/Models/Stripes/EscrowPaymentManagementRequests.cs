namespace EkofyApp.Application.Models.Stripes;
public sealed record ConfirmOrderCompletionRequest
{
    public string OrderId { get; init; } = null!;
    public List<string> DeliveryFiles { get; init; } = [];
    public string? DeliveryNotes { get; init; }
    public int? BuyerRating { get; init; } // 1-5 stars
    public string? BuyerReview { get; init; }
}

public sealed record RequestOrderRevisionRequest
{
    public string OrderId { get; init; } = null!;
    public string Feedback { get; init; } = null!;
    public List<string> FeedbackFiles { get; init; } = [];
}

public sealed record ReleaseEscrowPaymentRequest
{
    public string EscrowTransactionId { get; init; } = null!;
    public string? AdminNotes { get; init; }
    public bool ForceRelease { get; init; } = false; // Admin force release in case of dispute
}