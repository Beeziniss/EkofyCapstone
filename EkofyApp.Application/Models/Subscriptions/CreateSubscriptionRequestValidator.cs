using FluentValidation;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(100).WithMessage("DisplayName must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be a non-negative value.");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Invalid subscription tier.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid subscription status.");

        //RuleForEach(x => x.Entitlements)
        //    .SetValidator(new CreateEntitlementRequestValidator());
    }
}
