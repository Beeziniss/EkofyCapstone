using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Listeners;
public sealed class UpdateListenerRequestValidator : AbstractValidator<UpdateListenerRequest>
{
    public UpdateListenerRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .Matches(HelperMethod.RegexPatternAlphaNumericWithSpecific()).WithMessage("DisplayName must contain only alphanumeric characters, spaces, and underscores.")
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

        RuleFor(x => x.PhoneNumber)
            .Matches(HelperMethod.RegexPatternPhoneNumber()).WithMessage("PhoneNumber must be a valid phone number.")
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.FullName)
            .Matches(HelperMethod.RegexPatternAlphaWithSpace()).WithMessage("FullName must contain only alphabetic characters and spaces.")
            .MaximumLength(100).WithMessage("FullName cannot exceed 100 characters.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.Gender)
            .IsInEnum().When(x => x.Gender != null).WithMessage("Gender must be valid value");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Date of Birth is required")
            .Must(date => date != null && HelperMethod.GetExactAge(date.Value) >= 13).WithMessage("Date of Birth must be at least 13 years old");
    }
}
