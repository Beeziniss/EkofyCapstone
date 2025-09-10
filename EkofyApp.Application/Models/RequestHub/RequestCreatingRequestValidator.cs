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

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");
        }
    }
}
