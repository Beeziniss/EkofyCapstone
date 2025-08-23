using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.UserSubscriptions;
public sealed class UpdateUserSubscriptionRequestValidator : AbstractValidator<UpdateUserSubscriptionRequest>
{
    public UpdateUserSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("Subscription ID is required.")
            .MaximumLength(50).WithMessage("Subscription ID must not exceed 50 characters.");

        RuleFor(x => x.CancelAtEndOfPeriod)
            .NotEmpty().WithMessage("CancelAtEndOfPeriod must not be empty.")
            .Must(x => x == true || x == false).WithMessage("CancelAtEndOfPeriod must be a boolean value.");

        RuleFor(x => x.CanceledAt)
            .NotEmpty().WithMessage("CanceledAt must not be empty.")
            .LessThanOrEqualTo(HelperMethod.GetUtcPlus7TimeOffset()).WithMessage("Cancel time must be in the present or past");
    }
}
