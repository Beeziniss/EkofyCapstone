using EkofyApp.Application.Models.Albums;
using FluentValidation;

namespace EkofyApp.Application.Models.Albums;

public sealed class UpdateAlbumRequestValidator : AbstractValidator<UpdateAlbumRequest>
{
    public UpdateAlbumRequestValidator()
    {
        RuleFor(x => x.AlbumId)
            .NotEmpty()
            .WithMessage("Album ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("Album name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Album description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid album type.")
            .When(x => x.Type.HasValue);

        RuleFor(x => x.ArtistInfos)
            .Must(artistInfos => artistInfos!.All(a => !string.IsNullOrWhiteSpace(a.ArtistId)))
            .WithMessage("All artist IDs must be valid.")
            .When(x => x.ArtistInfos != null);

        RuleFor(x => x.CoverImage)
            .Must(BeAValidUrl)
            .WithMessage("Cover image must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.CoverImage));

        RuleFor(x => x.ThumbnailImage)
            .Must(BeAValidUrl)
            .WithMessage("Thumbnail image must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.ThumbnailImage));

        // Ensure at least one field is being updated
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Name) || 
                      x.Description != null || 
                      x.Type.HasValue ||
                      x.ArtistInfos != null ||
                      x.CoverImage != null ||
                      x.ThumbnailImage != null ||
                      x.ReleaseInfo != null ||
                      x.IsVisible.HasValue)
            .WithMessage("At least one field must be provided for update.");
    }

    private static bool BeAValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}