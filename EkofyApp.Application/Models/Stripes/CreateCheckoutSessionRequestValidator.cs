using EkofyApp.Domain.Enums;
using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateCheckoutSessionRequestValidator : AbstractValidator<CreateSubscriptionCheckoutSessionRequest>
{
    public CreateCheckoutSessionRequestValidator()
    {
        RuleFor(x => x.SubscriptionCode)
            .NotEmpty().WithMessage("Subscription Code is required.")
            .MaximumLength(50).WithMessage("Subscription Code must not exceed 50 characters.");

        //RuleFor(x => x.SubscriptionTier)
        //    .IsInEnum().WithMessage("Invalid subscription tier.");

        //RuleFor(x => x.SubscriptionVersion)
        //    .GreaterThan(0).WithMessage("Subscription version must be greater than 0.");

        RuleFor(x => x.Period)
            //.NotEmpty().WithMessage("Period is required")
            .IsInEnum().WithMessage("Must be valid data");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("Success URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Success URL must be a valid absolute URL.");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("Cancel URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Cancel URL must be a valid absolute URL.");

        //RuleFor(x => x.CouponCodes)
        //    .ForEach(code =>
        //    {
        //        code.MaximumLength(50).WithMessage("Coupon code must not exceed 50 characters.");
        //        code.NotEmpty().WithMessage("Coupon code must not be empty.");
        //    })
        //    .NotEmpty().When(x => x.Period == PeriodTime.year).WithMessage("Coupon Code is required for yearly");
    }
}
