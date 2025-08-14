using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed class CreateEntitlementRequestValidator : AbstractValidator<CreateEntitlementRequest>
{
    public CreateEntitlementRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Entitlements name is required.")
            .MaximumLength(100).WithMessage("Entitlements name must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Entitlements code is required.")
            .MaximumLength(50).WithMessage("Entitlements code must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Entitlements description is required.")
            .MaximumLength(500).WithMessage("Entitlements description must not exceed 500 characters.");

        RuleFor(x => x.ValueType)
            .IsInEnum().WithMessage("Invalid feature value type.");

        RuleFor(x => x.Value)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.String)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.Int)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.Double)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.Decimal)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.Boolean)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.DateTime)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.Array)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.EntitlementValueType.Object)
            .NotNull().When(x => x.ValueType != Domain.Enums.EntitlementValueType.Boolean)
            .WithMessage("Value cannot be null for non-boolean feature types.");

        RuleFor(x => x.ExpiredAt)
            .GreaterThan(HelperMethod.GetUtcPlus7Time()).When(x => x.ExpiredAt.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }
}
