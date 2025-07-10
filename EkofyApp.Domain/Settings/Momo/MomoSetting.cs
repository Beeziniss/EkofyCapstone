namespace EkofyApp.Domain.Settings.Momo;
public sealed record class MomoSetting
{
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string PartnerCode { get; init; }
    public required string ReturnUrl { get; init; }
    public required string NotifyUrl { get; init; }
    public required string RequestTypeQR { get; init; }
    public required string RequestTypeVisa { get; init; }
}
