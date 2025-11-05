using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;

public sealed class UpdatePriceRequestValidator : AbstractValidator<UpdatePriceRequest>
{
    public UpdatePriceRequestValidator()
    {
 RuleFor(x => x.StripePriceId)
       .NotEmpty().WithMessage("Stripe Price ID is required.")
            .MaximumLength(100).WithMessage("Stripe Price ID must not exceed 100 characters.");

        RuleFor(x => x.LookupKey)
            .MaximumLength(100).WithMessage("Lookup key must not exceed 100 characters.")
          .When(x => !string.IsNullOrEmpty(x.LookupKey));

        RuleFor(x => x.Interval)
     .IsInEnum().WithMessage("Interval must be one of the following values: day, week, month, or year.")
      .When(x => x.Interval.HasValue);

  RuleFor(x => x.IntervalCount)
  .GreaterThan(0).WithMessage("Interval count must be greater than 0.")
   .When(x => x.IntervalCount.HasValue);

      RuleForEach(x => x.Metadata)
  .Must(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .WithMessage("Metadata keys and values must not be empty or whitespace.")
            .When(x => x.Metadata != null && x.Metadata.Count != 0);
    }
}