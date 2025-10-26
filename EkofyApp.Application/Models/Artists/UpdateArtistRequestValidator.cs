using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Artists;
public sealed class UpdateArtistRequestValidator : AbstractValidator<UpdateArtistRequest>
{
    public UpdateArtistRequestValidator()
    {
        RuleFor(x => x.StageName)
            .MaximumLength(100).WithMessage("StageName must not exceed 100 characters.");

        RuleFor(x => x.Biography)
            .MaximumLength(10000).WithMessage("Bio must not exceed 10000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Biography));

        RuleFor(x => x.AvatarImage)
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("AvatarImage must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.AvatarImage));

        RuleFor(x => x.BannerImage)
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("BannerImage must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.BannerImage));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(HelperMethod.RegexPatternPhoneNumber()).WithMessage("PhoneNumber must be a valid phone number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.FullName)
            .Matches(HelperMethod.RegexPatternAlphaWithSpace()).WithMessage("FullName contains invalid characters.")
            .MaximumLength(200).WithMessage("FullName must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Gender)
            .IsInEnum().When(x => x.Gender != null).WithMessage("Gender must be valid value");

        RuleFor(x => x.BirthDate)
            .Must(date => date != null && HelperMethod.GetExactAge(date.Value) >= 18).WithMessage("Date of Birth must be at least 18 years old")
            .GreaterThan(DateTimeOffset.MinValue).WithMessage("Date of Birth must be a valid date")
            .When(x => x.BirthDate != null);
    }
}
