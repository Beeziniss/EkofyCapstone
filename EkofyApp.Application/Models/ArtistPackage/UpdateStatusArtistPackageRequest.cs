using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public class UpdateStatusArtistPackageRequest
    {
        public string Id { get; set; } = null!;
        public ArtistPackageStatus Status { get; set; }
    }
}
