using EkofyApp.Domain.Enums.Users;
using FluentValidation;

namespace EkofyApp.Application.Models.Entitlements;
public sealed class CreateEntitlementRequestValidator : AbstractValidator<CreateEntitlementRequest>
{
    public CreateEntitlementRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Entitlement name is required.")
            .MaximumLength(100).WithMessage("Entitlement name must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Entitlement code is required.")
            .MaximumLength(100).WithMessage("Entitlement code must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Entitlement description is required.")
            .MaximumLength(500).WithMessage("Entitlement description must not exceed 500 characters.");

        RuleFor(x => x.ValueType)
            .IsInEnum().WithMessage("Invalid entitlement value type.");

        RuleFor(x => x.DefaultValues)
            .NotNull().WithMessage("Default values list cannot be null.")
            .Must(list => list.All(dv => Enum.IsDefined(typeof(UserRole), dv.Role) && dv.Value != null))
            .WithMessage("Each default value must have a valid role and value.");

        RuleFor(x => x.SubscriptionOverrides)
            .NotNull().WithMessage("Subscription overrides list cannot be null.")
            .Must(list => list.All(so => !string.IsNullOrWhiteSpace(so.SubscriptionCode) && so.Value != null))
            .WithMessage("Each subscription override must have a valid subscription code and value.");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive flag is required.");
    }
}
