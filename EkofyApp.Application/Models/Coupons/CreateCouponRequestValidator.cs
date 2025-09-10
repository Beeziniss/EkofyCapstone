using FluentValidation;

namespace EkofyApp.Application.Models.Coupons;
public sealed class CreateCouponRequestValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");

        RuleFor(x => x.PercentOff)
            .GreaterThan(0).WithMessage("Percent off must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Percent off must be less than or equal to 100.");

        RuleFor(x => x.Duration)
            .IsInEnum().WithMessage("Invalid duration type.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status type.");
    }
}
