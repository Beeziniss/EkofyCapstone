using FluentValidation;

namespace EkofyApp.Application.Models.Works;
public sealed class CreateWorkSplitRequestValidator : AbstractValidator<CreateWorkSplitRequest>
{
    public CreateWorkSplitRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .MaximumLength(24).WithMessage("Recording ID must not exceed 24 characters.");

        RuleFor(x => x.ArtistRole)
            .IsInEnum().WithMessage("Must be correct value");

        RuleFor(x => x.Percentage)
            .NotEmpty().WithMessage("Percentage is required")
            .GreaterThanOrEqualTo(0).WithMessage("Must greater than or equal to 0")
            .LessThanOrEqualTo(100).WithMessage("Must be less than or equal to 100");
    }
}
