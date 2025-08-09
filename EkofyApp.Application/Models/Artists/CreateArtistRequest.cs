namespace EkofyApp.Application.Models.Artists;
public sealed record class CreateArtistRequest
{
    public string Name { get; init; }
    public string Biography { get; init; }
}
