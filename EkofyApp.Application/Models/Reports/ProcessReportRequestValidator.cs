using EkofyApp.Domain.Enums.Reports;
using FluentValidation;

namespace EkofyApp.Application.Models.Reports;

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

        RuleFor(x => x.RestrictionActionDetails)
            .NotEmpty().WithMessage("At least one restriction action must be specified")
            .When(x => x.ActionTaken == ReportAction.EntitlementRestriction);

        RuleFor(x => x.SuspensionDays)
            .GreaterThan(0).WithMessage("Suspension days must be greater than 0")
            .LessThanOrEqualTo(365).WithMessage("Suspension days cannot exceed 365 days")
            .When(x => x.SuspensionDays.HasValue);

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Note is required")
            .Unless(x => x.ActionTaken == ReportAction.NoAction || x.ActionTaken == ReportAction.EntitlementRestriction);

    }
}
