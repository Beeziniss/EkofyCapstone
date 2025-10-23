using FluentValidation;

namespace EkofyApp.Application.Models.UserEngagements;
public sealed class UserEngagementRequestValidator : AbstractValidator<UserEngagementRequest>
{
    public UserEngagementRequestValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("TargetId is required.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("TargetType is required")
            .IsInEnum().WithMessage("Invalid TargetType.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required")
            .IsInEnum().WithMessage("Invalid Action.");
    }
}
