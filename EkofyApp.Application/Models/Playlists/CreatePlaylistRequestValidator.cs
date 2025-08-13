using FluentValidation;

namespace EkofyApp.Application.Models.Playlists;
public sealed class CreatePlaylistRequestValidator : AbstractValidator<CreatePlaylistRequest>
{
    public CreatePlaylistRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters long")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.CoverImage)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Cover Image URL must be a valid URL");

        RuleFor(x => x.IsPublic)
            .Must(value => value == true || value == false).WithMessage("IsPublic must be a boolean value (true or false)");
    }
}
