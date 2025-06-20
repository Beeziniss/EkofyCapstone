namespace EkofyApp.Domain.Settings.AWS
{
    public class AWSSetting
    {
        public required string BucketName { get; set; }
        public required string Region { get; set; }
        public required string MediaConvertRole { get; set; }
        public required string MediaConvertEndpoint { get; set; }
        public required string MediaConvertQueue { get; set; } // Queue ARN
    }
}
