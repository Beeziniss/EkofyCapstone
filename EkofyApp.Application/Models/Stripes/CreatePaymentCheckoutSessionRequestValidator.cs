using EkofyApp.Application.Models.ArtistPackage;
using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreatePaymentCheckoutSessionRequestValidator : AbstractValidator<CreatePaymentCheckoutSessionRequest>
{
    public CreatePaymentCheckoutSessionRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("Package ID is required.");

        RuleFor(x => x.RequestHubId)
            .NotEmpty().WithMessage("Request Hub ID is required.");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("Success URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Success URL must be a valid absolute URL.");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("Cancel URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Cancel URL must be a valid absolute URL.");

        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(x => x.Deadline)
            .GreaterThan(x => HelperMethod.GetUtcPlus7TimeOffset()).WithMessage("Deadline must be a future date.");
    }
}
