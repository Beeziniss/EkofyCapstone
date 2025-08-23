using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Auth.Artists;
public sealed class CreateIdentityCardRequestValidator : AbstractValidator<CreateIdentityCardRequest>
{
    public CreateIdentityCardRequestValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Identity Card Number is required")
            //.MinimumLength(9).WithMessage("Identity Card Number must be at least 9 characters long")
            //.MaximumLength(12).WithMessage("Identity Card Number must not exceed 12 characters")
            .Matches(HelperMethod.RegexPatternIdentityCardNumber()).WithMessage("Identity Card Number must contain only 9 or 12 digits");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full Name is required")
            .MinimumLength(3).WithMessage("Full Name must be at least 3 characters long")
            .MaximumLength(50).WithMessage("Full Name must not exceed 100 characters")
            .Matches(HelperMethod.RegexPatternAlphaWithSpace()).WithMessage("Full Name must contain only letters and spaces");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of Birth is required")
            .Must(date => HelperMethod.GetExactAge(date) >= 14).WithMessage("Date of Birth must be at least 14 years old")
            .GreaterThan(DateTimeOffset.MinValue).WithMessage("Date of Birth must be a valid date");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender must be Male or Female or Other");

        RuleFor(x => x.PlaceOfOrigin)
            .NotEmpty().WithMessage("Place of Origin is required")
            .MinimumLength(3).WithMessage("Place of Origin must be at least 3 characters long")
            .MaximumLength(100).WithMessage("Place of Origin must not exceed 100 characters")
            .Matches(HelperMethod.RegexPatternAlphaNumericWithSpecific()).WithMessage("Place of Origin must contain only letters and spaces");

        RuleFor(x => x.Nationality)
            .NotEmpty().WithMessage("Nationality is required")
            .Matches(HelperMethod.RegexPatternAlphaNumericWithSpace()).WithMessage("Nationality must contain only letters and spaces");

        RuleFor(x => x.PlaceOfResidence)
            .NotEmpty().WithMessage("Place of Residence is required");
            //.SetValidator(new AddressValidator()).WithMessage("Invalid Place of Residence details");

        RuleFor(x => x.FrontImage)
            .NotEmpty().WithMessage("Front Image URL is required")
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Front Image URL must be a valid URL");

        RuleFor(x => x.BackImage)
            .NotEmpty().WithMessage("Back Image URL is required")
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Back Image URL must be a valid URL");

        RuleFor(x => x.ValidUntil)
            .GreaterThanOrEqualTo(x => HelperMethod.NormalizeToUtcPlus7TimeOffset(x.ValidUntil)).WithMessage("Valid Until date must be in the future");
    }
}
