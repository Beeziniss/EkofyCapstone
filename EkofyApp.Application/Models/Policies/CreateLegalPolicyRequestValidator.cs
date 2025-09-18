using FluentValidation;

namespace EkofyApp.Application.Models.Policies;
public sealed class CreateLegalPolicyRequestValidator : AbstractValidator<CreateLegalPolicyRequest>
{
    public CreateLegalPolicyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Policy name is required.")
            .MaximumLength(200).WithMessage("Policy name must not exceed 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Policy content is required.");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive field is required.");
    }
}
