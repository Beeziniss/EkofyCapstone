using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EkofyApp.Application.Models.Subscriptions;
public sealed class UpdateMetdataSubscriptionRequestValidator : AbstractValidator<UpdateMetdataSubscriptionRequest>
{
    public UpdateMetdataSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty().WithMessage("SubscriptionId is required.")
            .MaximumLength(100).WithMessage("SubscriptionId must not exceed 100 characters.");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("PackageDescription must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Currency must be a valid currency type.")
            .When(x => x.Currency.HasValue);
    }
}
