using EkofyApp.Application.Models.Albums;
using FluentValidation;

namespace EkofyApp.Application.Models.Albums;

public sealed class CreateAlbumRequestValidator : AbstractValidator<CreateAlbumRequest>
{
    public CreateAlbumRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Album name is required.")
            .MaximumLength(200)
            .WithMessage("Album name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Album description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid album type.");

        RuleFor(x => x.TrackIds)
            .Must(trackIds => trackIds != null && trackIds.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("All track IDs must be valid.")
            .When(x => x.TrackIds != null && x.TrackIds.Any());

        RuleFor(x => x.ArtistInfos)
            .NotEmpty()
            .WithMessage("At least one artist must be specified for the album.")
            .Must(artistInfos => artistInfos.All(a => !string.IsNullOrWhiteSpace(a.ArtistId)))
            .WithMessage("All artist IDs must be valid.");

        RuleFor(x => x.CoverImage)
            .Must(BeAValidUrl)
            .WithMessage("Cover image must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.CoverImage));

        RuleFor(x => x.ThumbnailImage)
            .Must(BeAValidUrl)
            .WithMessage("Thumbnail image must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.ThumbnailImage));

        RuleFor(x => x.ReleaseInfo)
            .NotNull()
            .WithMessage("Release information is required.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}