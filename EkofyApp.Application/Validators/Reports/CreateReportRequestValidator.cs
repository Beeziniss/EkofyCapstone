using EkofyApp.Application.Models.Reports;
using FluentValidation;

namespace EkofyApp.Application.Validators.Reports;

public sealed class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.ReportedUserId)
            .NotEmpty().WithMessage("Reported user ID is required")
            .Length(24).WithMessage("Invalid user ID format");

        RuleFor(x => x.ReportType)
            .IsInEnum().WithMessage("Invalid report type");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.RelatedContentId)
            .Length(24).WithMessage("Invalid content ID format")
            .When(x => !string.IsNullOrEmpty(x.RelatedContentId));

        RuleFor(x => x.RelatedContentType)
            .IsInEnum().WithMessage("Invalid related content type")
            .When(x => !string.IsNullOrEmpty(x.RelatedContentType.ToString()));

        RuleFor(x => x.Evidences)
            .Must(urls => urls == null || urls.Count <= 5)
            .WithMessage("Cannot upload more than 5 evidence URLs");
    }
}
