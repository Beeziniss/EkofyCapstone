using EkofyApp.Application.Models.Albums;
using FluentValidation;

namespace EkofyApp.Application.Models.Albums;

public sealed class AddTrackToAlbumRequestValidator : AbstractValidator<AddTrackToAlbumRequest>
{
    public AddTrackToAlbumRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty()
            .WithMessage("Track ID is required.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.AlbumId) || !string.IsNullOrEmpty(x.AlbumName))
            .WithMessage("Either Album ID or Album Name must be provided.");

        RuleFor(x => x.AlbumName)
            .MaximumLength(200)
            .WithMessage("Album name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.AlbumName));
    }
}