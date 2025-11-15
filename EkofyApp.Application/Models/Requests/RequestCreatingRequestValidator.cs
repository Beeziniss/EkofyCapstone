using FluentValidation;

namespace EkofyApp.Application.Models.Requests
{
    public class RequestCreatingRequestValidator : AbstractValidator<RequestCreatingRequest>
    {
        public RequestCreatingRequestValidator()
        {
            RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

            RuleFor(x => x.Summary)
                .NotEmpty().WithMessage("Summary is required.");

            RuleFor(x => x.DetailDescription)
                .NotEmpty().WithMessage("Detail PackageDescription is required.")
                .Length(0, 1000)
                .WithMessage("Detail PackageDescription must not exceed 1000 characters.");
            RuleFor(x => x.Budget.Min)
                .GreaterThanOrEqualTo(0).WithMessage("Budget must be over than 0.");
            RuleFor(x => x.Budget.Max)
                .GreaterThanOrEqualTo(0).WithMessage("Budget must be over than 0.");
        }
    }
}
