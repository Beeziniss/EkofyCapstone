using EkofyApp.Application.Models.Reports;
using FluentValidation;

namespace EkofyApp.Application.Validators.Reports;

public sealed class ProcessReportRequestValidator : AbstractValidator<ProcessReportRequest>
{
    public ProcessReportRequestValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty().WithMessage("Report ID is required")
            .Length(24).WithMessage("Invalid report ID format");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid report status");

        RuleFor(x => x.ActionTaken)
            .IsInEnum().WithMessage("Invalid action type");

        RuleFor(x => x.SuspensionDays)
            .GreaterThan(0).WithMessage("Suspension days must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Suspension days cannot exceed 365 days")
            .When(x => x.SuspensionDays.HasValue);

        RuleFor(x => x.ModeratorNotes)
            .MaximumLength(2000).WithMessage("Moderator notes cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.ModeratorNotes));
    }
}
