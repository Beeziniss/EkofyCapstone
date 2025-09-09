using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateCheckoutSessionRequestValidator : AbstractValidator<CreateCheckoutSessionRequest>
{
    public CreateCheckoutSessionRequestValidator()
    {
        RuleFor(x => x.SubscriptionTier)
            .IsInEnum().WithMessage("Invalid subscription tier.");

        RuleFor(x => x.SubscriptionVersion)
            .GreaterThan(0).WithMessage("Subscription version must be greater than 0.");

        RuleFor(x => x.Period)
            .NotEmpty().WithMessage("Period is required")
            .IsInEnum().WithMessage("Must be valid data");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("Success URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Success URL must be a valid absolute URL.");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("Cancel URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Cancel URL must be a valid absolute URL.");
    }
}
