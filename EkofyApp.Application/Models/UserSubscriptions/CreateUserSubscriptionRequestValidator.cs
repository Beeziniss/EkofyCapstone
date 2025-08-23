using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.UserSubscriptions;
public sealed class CreateUserSubscriptionRequestValidator : AbstractValidator<CreateUserSubscriptionRequest>
{
    public CreateUserSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("Subscription ID is required.")
            .MaximumLength(50).WithMessage("Subscription ID must not exceed 50 characters.");

        RuleFor(x => x.PeriodStart)
            .NotEmpty().WithMessage("Period start date is required.")
            .LessThanOrEqualTo(HelperMethod.GetUtcPlus7TimeOffset()).WithMessage("Period start date must be in the present.")
            .LessThanOrEqualTo(x => x.PeriodEnd).WithMessage("Period start date must be before or equal to period end date.");

        RuleFor(x => x.PeriodEnd)
            .NotEmpty().WithMessage("Period end date is required.")
            .GreaterThanOrEqualTo(HelperMethod.GetUtcPlus7TimeOffset()).WithMessage("Period end date must be in the future or present.");
    }
}
