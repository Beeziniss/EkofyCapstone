using EkofyApp.Application.Models.ArtistPackage;
using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreatePaymentCheckoutSessionRequestValidator : AbstractValidator<CreatePaymentCheckoutSessionRequest>
{
    public CreatePaymentCheckoutSessionRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required.");

        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required.");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("Success URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Success URL must be a valid absolute URL.");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("Cancel URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Cancel URL must be a valid absolute URL.");

        RuleFor(x => x.Duration)
            .GreaterThan(x => 0).WithMessage("Duration must be greater than zero.");
    }
}
