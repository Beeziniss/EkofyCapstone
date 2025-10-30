using FluentValidation;

namespace EkofyApp.Application.Models.Policies;
public sealed class UpdateRoyalPolicyRequestValidator : AbstractValidator<UpdateRoyalPolicyRequest>
{
    public UpdateRoyalPolicyRequestValidator()
    {
        RuleFor(x => x.Version)
            .GreaterThan(0).WithMessage("Version must be greater than 0.");

        RuleFor(x => x.RatePerStream)
            .GreaterThanOrEqualTo(0).When(x => x.RatePerStream.HasValue)
            .WithMessage("Rate per stream must be greater than or equal to 0.");

        RuleFor(x => x.RecordingPercentage)
            .InclusiveBetween(0, 100).When(x => x.RecordingPercentage.HasValue)
            .WithMessage("Recording percentage must be between 0 and 100.");

        RuleFor(x => x.WorkPercentage)
            .InclusiveBetween(0, 100).When(x => x.WorkPercentage.HasValue)
            .WithMessage("Work percentage must be between 0 and 100.");
    }
}
