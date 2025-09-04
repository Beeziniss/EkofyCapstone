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

        RuleFor(x => x.Version)
            .GreaterThan(0).WithMessage("Version must be greater than 0.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be a non-negative value.");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Invalid subscription tier.");

        RuleForEach(x => x.Entitlements)
            .SetValidator(new CreateEntitlementRequestValidator());
    }
}
