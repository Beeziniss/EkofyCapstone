using FluentValidation;

namespace EkofyApp.Application.Models.Reviews;
public sealed class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("ReviewId is required.");

        When(x => x.Rating.HasValue, () =>
        {
            RuleFor(x => x.Rating!.Value)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        });

        When(x => x.Comment != null, () =>
        {
            RuleFor(x => x.Comment!)
                .NotEmpty().WithMessage("Comment cannot be empty.")
                .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
        });
    }
}
