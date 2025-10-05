using FluentValidation;

namespace EkofyApp.Application.Models.Listeners;
public sealed class UpdateListenerRequestValidator : AbstractValidator<UpdateListenerRequest>
{
    public UpdateListenerRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(100).WithMessage("DisplayName cannot exceed 100 characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.AvatarImage)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Avatar Image URL must be a valid URL")
            .When(x => x.AvatarImage is not null);

        RuleFor(x => x.BannerImage)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Banner Image URL must be a valid URL")
            .When(x => x.BannerImage is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => x.Email is not null);

        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("FullName cannot exceed 100 characters.")
            .When(x => x.FullName is not null);
    }
}
