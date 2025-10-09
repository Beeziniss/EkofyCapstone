using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Artists;

public sealed class ArtistRegistrationApprovalRequestValidator : AbstractValidator<ArtistRegistrationApprovalRequest>
{
    public ArtistRegistrationApprovalRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Artist registration ID is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full DisplayName is required")
            .Matches(HelperMethod.RegexPatternAlphaNumericWithSpace()).WithMessage("Full DisplayName must contain only letters and spaces");

        //RuleFor(x => x.Approved)
        //    .NotNull().WithMessage("Approval status is required");

        //RuleFor(x => x.RejectionReason)
        //    .NotEmpty().WithMessage("Rejection reason is required when rejecting an application")
        //    .When(x => !x.Approved);

        RuleFor(x => x.RejectionReason)
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.RejectionReason));
    }
}