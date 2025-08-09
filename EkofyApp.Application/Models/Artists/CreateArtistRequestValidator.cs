using FluentValidation;

namespace EkofyApp.Application.Models.Artists;
public sealed class CreateArtistRequestValidator : AbstractValidator<CreateArtistRequest>
{
    public CreateArtistRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100);

        RuleFor(x => x.Biography)
            .MaximumLength(2000);
    }
}
