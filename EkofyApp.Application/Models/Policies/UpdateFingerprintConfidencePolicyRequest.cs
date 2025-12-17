namespace EkofyApp.Application.Models.Policies;

public sealed record class UpdateFingerprintConfidencePolicyRequest
{
    public double RejectThreshold { get; init; }
    public double ManualReviewThreshold { get; init; }
}
