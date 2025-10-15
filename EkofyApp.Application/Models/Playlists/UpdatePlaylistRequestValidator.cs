using FluentValidation;

namespace EkofyApp.Application.Models.Playlists;
public sealed class UpdatePlaylistRequestValidator : AbstractValidator<UpdatePlaylistRequest>
{
    public UpdatePlaylistRequestValidator()
    {
        RuleFor(x => x.PlaylistId)
            .NotEmpty().WithMessage("Playlist ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("DisplayName is required")
            .MinimumLength(3).WithMessage("DisplayName must be at least 3 characters long")
            .MaximumLength(100).WithMessage("DisplayName must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.CoverImage)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Cover Image URL must be a valid URL")
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImage));

        RuleFor(x => x.IsPublic)
            .Must(value => value == true || value == false).WithMessage("IsPublic must be a boolean value (true or false)")
            .When(x => x.IsPublic != null);
    }
}
