using EkofyApp.Domain.Utils;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateSubScriptionPlanRequestValidator : AbstractValidator<CreateSubScriptionPlanRequest>
{
    public CreateSubScriptionPlanRequestValidator()
    {
        RuleFor(x => x.LookupKey)
            .NotEmpty().WithMessage("Lookup key is required.")
            .MaximumLength(100).WithMessage("Lookup key must not exceed 100 characters.")
            .Matches(x => HelperMethod.RegexPatternAlphaNumericWithSpecific());

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subscription name is required.")
            .MaximumLength(100).WithMessage("Subscription name must not exceed 100 characters.")
            .Matches(x => HelperMethod.RegexPatternAlphaNumericWithSpace());

        //RuleFor(x => x.UnitAmount)
        //    .NotEmpty().WithMessage("UnitAmount is required.")
        //    .GreaterThan(0).WithMessage("UnitAmount must be greater than 0.");

        RuleFor(x => x.Interval)
            .NotEmpty().WithMessage("Interval is required.")
            .Must(interval => new[] { "day", "week", "month", "year" }.Contains(interval))
            .WithMessage("Interval must be one of the following values: day, week, month, or year.");

        RuleFor(x => x.IntervalCount)
            .NotEmpty().WithMessage("Interval count is required.")
            .GreaterThan(0).WithMessage("Interval count must be greater than 0.");

        RuleForEach(x => x.Images)
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Each image URL must be a valid absolute URL.")
            .When(x => x.Images != null && x.Images.Count != 0);

        RuleForEach(x => x.Metadata)
            .Must(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .WithMessage("Metadata keys and values must not be empty or whitespace.")
            .When(x => x.Metadata != null && x.Metadata.Count != 0);

        RuleFor(x => x.SubscriptionTier)
            .IsInEnum().WithMessage("Subscription Tier must be a valid enum value.");

        RuleFor(x => x.SubscriptionVersion)
            .NotEmpty().WithMessage("Subscription Version is required.")
            .GreaterThan(0).WithMessage("Subscription Version must be greater than 0.");
    }
}
