using FluentValidation;

namespace EkofyApp.Application.Models.Artists;
public sealed class UpdateArtistRequestValidator : AbstractValidator<UpdateArtistRequest>
{
    public UpdateArtistRequestValidator()
    {
        RuleFor(x => x.StageName)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(100).WithMessage("DisplayName must not exceed 100 characters.");

        RuleFor(x => x.Biography)
            .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Biography));

        RuleFor(x => x.AvatarImage)
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("AvatarImage must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.AvatarImage));

        RuleFor(x => x.BannerImage)
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("BannerImage must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.BannerImage));
    }
}
