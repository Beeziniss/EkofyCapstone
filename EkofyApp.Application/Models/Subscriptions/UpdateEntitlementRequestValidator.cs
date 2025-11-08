using EkofyApp.Domain.Enums;
using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed class UpdateEntitlementRequestValidator : AbstractValidator<UpdateEntitlementRequest>
{
    public UpdateEntitlementRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.Code) && x.ValueType != null && x.Value != null && !string.IsNullOrWhiteSpace(x.Description)).WithMessage("Name is required when Code, ValueType or Value is provided.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        RuleFor(x => x.Description)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.Code) && x.ValueType != null && x.Value != null && !string.IsNullOrWhiteSpace(x.Name)).WithMessage("PackageDescription is required when Code, ValueType or Value is provided.")
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("PackageDescription must not exceed 500 characters.");

        RuleFor(x => x.ValueType)
            //.NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name) && x.Value != null && !string.IsNullOrWhiteSpace(x.PackageDescription)).WithMessage("ValueType is required when Code or Value is provided.")
            .IsInEnum().WithMessage("ValueType must be a valid type.");

        RuleFor(x => x.Value)
            .NotNull().When(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name) && x.ValueType != null && !string.IsNullOrWhiteSpace(x.Description)).WithMessage("Value is required when Code or ValueType is provided.")
            .Must((request, value) =>
            {
                return request.ValueType switch
                {
                    EntitlementValueType.String => value is string,
                    EntitlementValueType.Decimal => value is decimal,
                    EntitlementValueType.Double => value is double,
                    EntitlementValueType.Int => value is int,
                    EntitlementValueType.Long => value is long,
                    EntitlementValueType.Array => value is Array,
                    EntitlementValueType.Object => value is not null, // Accept any non-null object
                    EntitlementValueType.DateTime => value is DateTimeOffset,
                    EntitlementValueType.Boolean => value is bool,
                    _ => false,
                };
            }).WithMessage("Value must match the specified ValueType.");

        RuleFor(x => x.ExpiredAt)
            .GreaterThanOrEqualTo(x => HelperMethod.GetUtcPlus7TimeOffset()).When(x => x.ExpiredAt.HasValue).WithMessage("ExpiredAt must be a future date.");
    }
}
