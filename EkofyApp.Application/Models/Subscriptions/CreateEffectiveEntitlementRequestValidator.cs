using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed class CreateEffectiveEntitlementRequestValidator : AbstractValidator<CreateEffectiveEntitlementRequest>
{
    public CreateEffectiveEntitlementRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("Entitlements ID is required")
            .MaximumLength(50).WithMessage("Entitlements ID must not exceed 50 characters");

        RuleFor(x => x.SubscriptionCode)
            .MinimumLength(3).WithMessage("Entitlements Code must be at least 3 characters long")
            .MaximumLength(50).WithMessage("Entitlements Code must not exceed 50 characters")
            .Matches(HelperMethod.RegexPatternAlphaNumericWithSpecific()).WithMessage("Entitlements Code can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.SubscriptionVersion)
            .GreaterThan(0).WithMessage("Subscription Version must be greater than 0");

        RuleFor(x => x.ValidUntil)
            .GreaterThanOrEqualTo(x => HelperMethod.NormalizeToUtcPlus7TimeOffset(x.ValidUntil)).WithMessage("Expiration date must be in the future.");
    }
}
