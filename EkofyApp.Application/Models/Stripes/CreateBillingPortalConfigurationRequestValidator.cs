using EkofyApp.Domain.Enums.BillingPortalConfig;
using EkofyApp.Domain.Enums.Users;
using FluentValidation;

namespace EkofyApp.Application.Models.Stripes;
public sealed class CreateBillingPortalConfigurationRequestValidator : AbstractValidator<CreateBillingPortalConfigurationRequest>
{
    public CreateBillingPortalConfigurationRequestValidator()
    {
        RuleFor(x => x.CustomerUpdateEnabled)
            .NotNull().WithMessage("CustomerUpdateEnabled is required");

        RuleFor(x => x.AllowedCustomerUpdates)
            .NotNull().When(x => x.CustomerUpdateEnabled).WithMessage("AllowedCustomerUpdates must be provided when CustomerUpdateEnabled is true.")
            .Must(list => list != null && list.Count > 0).When(x => x.CustomerUpdateEnabled).WithMessage("AllowedCustomerUpdates must contain at least one item when CustomerUpdateEnabled is true.");

        RuleFor(x => x.UserRole)
            .IsInEnum().WithMessage("UserRole must be a valid enum value.")
            .Must(role => role == UserRole.Admin || role == UserRole.Moderator).WithMessage("Cannot create Billing Portal Configuration for Admin or Moderator role.");
        RuleFor(x => x.SubscriptionTier)
            .IsInEnum().WithMessage("SubscriptionTier must be a valid enum value.");
        RuleFor(x => x.Version)
            .GreaterThan(0).WithMessage("Version must be greater than 0.");

        RuleFor(x => x.PaymentMethodUpdateEnabled)
            .NotNull().WithMessage("PaymentMethodUpdateEnabled is required");

        RuleFor(x => x.InvoiceHistoryEnabled)
            .NotNull().WithMessage("InvoiceHistoryEnabled is required");

        RuleFor(x => x.SubscriptionCancelEnabled)
            .NotNull().WithMessage("SubscriptionCancelEnabled is required");

        RuleFor(x => x.Mode)
            .NotEmpty().When(x => x.SubscriptionCancelEnabled).WithMessage("Mode must be provided when SubscriptionCancelEnabled is true.")
            .Must(mode => mode == StripeSubscriptionCancelMode.Immediately || mode == StripeSubscriptionCancelMode.AtPeriodEnd).When(x => x.SubscriptionCancelEnabled).WithMessage("Mode must be either 'immediately' or 'at_period_end'.");

        RuleFor(x => x.SuscriptionUpdateEnabled)
            .NotNull().WithMessage("SuscriptionUpdateEnabled is required");

        RuleFor(x => x.AllowedSubscriptionUpdates)
            .NotNull().When(x => x.SuscriptionUpdateEnabled).WithMessage("AllowedSubscriptionUpdates must be provided when SuscriptionUpdateEnabled is true.")
            .Must(list => list != null && list.Count > 0).When(x => x.SuscriptionUpdateEnabled).WithMessage("AllowedSubscriptionUpdates must contain at least one item when SuscriptionUpdateEnabled is true.");

        RuleFor(x => x.Products)
            .NotNull().When(x => x.SuscriptionUpdateEnabled).WithMessage("Products must be provided when SuscriptionUpdateEnabled is true.")
            .Must(list => list != null && list.Count > 0).When(x => x.SuscriptionUpdateEnabled).WithMessage("Products must contain at least one item when SuscriptionUpdateEnabled is true.");

        RuleForEach(x => x.Products)
            .ChildRules(product =>
            {
                product.RuleFor(p => p.Id)
                    .NotEmpty().WithMessage("Product Id must not be empty.");

                product.RuleFor(p => p.StripePriceIds)
                    .NotNull().WithMessage("StripePriceIds must not be null.")
                    .Must(list => list != null && list.Count > 0).WithMessage("StripePriceIds must contain at least one item.");
            })
            .When(x => x.Products != null);
    }
}
