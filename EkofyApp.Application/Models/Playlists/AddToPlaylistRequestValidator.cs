using FluentValidation;

namespace EkofyApp.Application.Models.Playlists;
public sealed class AddToPlaylistRequestValidator : AbstractValidator<AddToPlaylistRequest>
{
    public AddToPlaylistRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty().WithMessage("Track ID is required.");

        RuleFor(x => x.PlaylistId)
            .NotNull().When(x => !string.IsNullOrWhiteSpace(x.PlaylistId)).WithMessage("Playlist ID must be not null.");

        RuleFor(x => x.PlaylistName)
            .NotEmpty().When(x => string.IsNullOrWhiteSpace(x.PlaylistId)).WithMessage("Playlist name is required when creating a new playlist.")
            .MaximumLength(100).WithMessage("Playlist name must not exceed 100 characters.")
            .NotEqual("Favorite Songs").WithMessage("Playlist name must not named Favorite Songs");
    }
}
