using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreatePriceRequestValidator : AbstractValidator<CreatePriceRequest>
{
    public CreatePriceRequestValidator()
    {
        RuleFor(x => x.LookupKey)
            .NotEmpty().WithMessage("Lookup key is required.")
            .MaximumLength(100).WithMessage("Lookup key must not exceed 100 characters.")
            .Matches(x => HelperMethod.RegexPatternAlphaNumericWithSpecific());

        RuleFor(x => x.Interval)
            //.NotEmpty().WithMessage("Interval is required.")
            .IsInEnum().WithMessage("Interval must be one of the following values: day, week, month, or year.");

        RuleFor(x => x.IntervalCount)
            .NotEmpty().WithMessage("Interval count is required.")
            .GreaterThan(0).WithMessage("Interval count must be greater than 0.");
    }
}
