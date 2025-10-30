using FluentValidation;

namespace EkofyApp.Application.Models.RequestHub
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
                .NotEmpty().WithMessage("Detail Description is required.")
                .Length(0, 1000)
                .WithMessage("Detail Description must not exceed 1000 characters.");
            RuleFor(x => x.Budget)
                .GreaterThanOrEqualTo(0).WithMessage("Budget must be over than 0.");
        }
    }
}
