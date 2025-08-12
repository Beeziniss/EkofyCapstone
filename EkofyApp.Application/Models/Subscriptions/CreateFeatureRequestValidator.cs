using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed class CreateFeatureRequestValidator : AbstractValidator<CreateFeatureRequest>
{
    public CreateFeatureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Feature name is required.")
            .MaximumLength(100).WithMessage("Feature name must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Feature code is required.")
            .MaximumLength(50).WithMessage("Feature code must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Feature description is required.")
            .MaximumLength(500).WithMessage("Feature description must not exceed 500 characters.");

        RuleFor(x => x.ValueType)
            .IsInEnum().WithMessage("Invalid feature value type.");

        RuleFor(x => x.Value)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.String)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.Int)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.Double)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.Decimal)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.Boolean)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.DateTime)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.Array)
            .NotEmpty().When(x => x.ValueType == Domain.Enums.FeatureValueType.Object)
            .NotNull().When(x => x.ValueType != Domain.Enums.FeatureValueType.Boolean)
            .WithMessage("Value cannot be null for non-boolean feature types.");

        RuleFor(x => x.ExpiredAt)
            .GreaterThan(HelperMethod.GetUtcPlus7Time()).When(x => x.ExpiredAt.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }
}
