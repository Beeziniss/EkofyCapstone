using FluentValidation;

namespace EkofyApp.Application.Models.Recordings;
public sealed class CreateRecordingSplitRequestValidator : AbstractValidator<CreateRecordingSplitRequest>
{
    public CreateRecordingSplitRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.")
            .MaximumLength(24).WithMessage("RecordingProjection ID must not exceed 24 characters.");

        RuleFor(x => x.ArtistRole)
            .IsInEnum().WithMessage("Must be correct value");

        RuleFor(x => x.Percentage)
            .NotEmpty().WithMessage("Percentage is required")
            .GreaterThanOrEqualTo(0).WithMessage("Must greater than or equal to 0")
            .LessThanOrEqualTo(100).WithMessage("Must be less than or equal to 100");
    }
}
