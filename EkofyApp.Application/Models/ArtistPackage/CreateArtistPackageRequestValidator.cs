using FluentValidation;

namespace EkofyApp.Application.Models.ArtistPackage
{
    public sealed class CreateArtistPackageRequestValidator : AbstractValidator<CreateArtistPackageRequest>
    {
        public CreateArtistPackageRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0");

            RuleFor(x => x.EstimateDeliveryDays)
                .GreaterThan(0).WithMessage("Estimated delivery days must be greater than 0");

            RuleFor(x => x.MaxRevision)
                .GreaterThan(0).WithMessage("Max revision must be greater than 0");

        }
    }
}
