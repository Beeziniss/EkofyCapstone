namespace EkofyApp.Domain.Settings.AWS
{
    public class AWSSetting
    {
        public required string BucketName { get; set; }
        public required string Region { get; set; }
        public required string CloudFrontDistributionId { get; set; }
        public required string CloudFrontDomainUrl { get; set; }
        public required string KeyPairId { get; set; }
    }
}
