using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;

public sealed class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator()
    {
    RuleFor(x => x.SubscriptionPlanId)
     .NotEmpty().WithMessage("Subscription plan ID is required.")
            .MaximumLength(50).WithMessage("Subscription plan ID must not exceed 50 characters.");

     RuleFor(x => x.NewPrices)
         .ForEach(price => price.SetValidator(new CreatePriceRequestValidator()))
 .WithMessage("Error while validating new prices");

  RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Subscription name must not exceed 100 characters.")
.Matches(x => HelperMethod.RegexPatternAlphaNumericWithSpace())
            .When(x => !string.IsNullOrEmpty(x.Name))
       .WithMessage("Subscription name contains invalid characters.");

        RuleForEach(x => x.Images)
     .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("Each image URL must be a valid absolute URL.")
        .When(x => x.Images != null && x.Images.Count != 0);

        RuleForEach(x => x.Metadata)
            .Must(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
          .WithMessage("Metadata keys and values must not be empty or whitespace.")
     .When(x => x.Metadata != null && x.Metadata.Count != 0);
    }
}