namespace EkofyApp.Domain.Settings.AWS;
public sealed record class AWSSetting
{
    public required string BucketName { get; init; }
    public required string Region { get; init; }
    public required string CloudFrontDistributionId { get; init; }
    public required string CloudFrontDomainUrl { get; init; }
    public required string KeyPairId { get; init; }
}
