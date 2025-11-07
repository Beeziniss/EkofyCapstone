using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Policies;
public sealed class CreateRoyalPolicyRequestValidator : AbstractValidator<CreateRoyalPolicyRequest>
{
    public CreateRoyalPolicyRequestValidator()
    {
        RuleFor(x => x.RatePerStream)
            .GreaterThan(0).WithMessage("Rate per stream must be greater than 0.");

        RuleFor(x => x.RecordingPercentage)
            .InclusiveBetween(0, 1).WithMessage("Recording percentage must be between 0 and 1.");

        RuleFor(x => x.WorkPercentage)
            .InclusiveBetween(0, 1).WithMessage("Work percentage must be between 0 and 1.");

        RuleFor(x => x)
            .Must(x => x.RecordingPercentage + x.WorkPercentage == 1).WithMessage("The sum of recording and work percentages must equal 1.");

        RuleFor(RuleFor => RuleFor.Currency)
            .IsInEnum().WithMessage("Invalid currency type.");

        //RuleFor(x => x.IsActive)
        //    .NotNull().WithMessage("IsActive must be specified.");

        //RuleFor(x => x.EffectiveAt)
        //    .GreaterThan(x => HelperMethod.GetUtcPlus7TimeOffset()).WithMessage("Effective date must be in the future.");
    }
}
