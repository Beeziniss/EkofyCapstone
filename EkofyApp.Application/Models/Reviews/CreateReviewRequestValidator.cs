using FluentValidation;

namespace EkofyApp.Application.Models.Reviews;
public sealed class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.PackageOrderId)
            .NotEmpty().WithMessage("PackageOrderId is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(1000).WithMessage("Content cannot exceed 1000 characters.");
    }
}
