using EkofyApp.Application.Models.Albums;
using FluentValidation;

namespace EkofyApp.Application.Models.Albums;

public sealed class RemoveTrackFromAlbumRequestValidator : AbstractValidator<RemoveTrackFromAlbumRequest>
{
    public RemoveTrackFromAlbumRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty()
            .WithMessage("Track ID is required.");

        RuleFor(x => x.AlbumId)
            .NotEmpty()
            .WithMessage("Album ID is required.");
    }
}