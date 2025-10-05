namespace EkofyApp.Application.Models.Artists;
public sealed record class UpdateArtistRequest
{
    public string? StageName { get; init; }
    public string? Biography { get; init; }
    public string? AvatarImage { get; init; }
    public string? BannerImage { get; init; }
}
