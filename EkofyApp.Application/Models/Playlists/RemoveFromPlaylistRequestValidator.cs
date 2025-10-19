using FluentValidation;

namespace EkofyApp.Application.Models.Playlists;
public sealed class RemoveFromPlaylistRequestValidator : AbstractValidator<RemoveFromPlaylistRequest>
{
    public RemoveFromPlaylistRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty().WithMessage("Track ID is required.");

        RuleFor(x => x.PlaylistId)
            .NotNull().When(x => !string.IsNullOrWhiteSpace(x.PlaylistId)).WithMessage("Playlist ID must be not null.");
    }
}
