using FluentValidation;

namespace EkofyApp.Application.Models.Works;
public sealed class CreateWorkRequestValidator : AbstractValidator<CreateWorkRequest>
{
    public CreateWorkRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.WorkSplits)
            .ForEach(x => x.SetValidator(new CreateWorkSplitRequestValidator()));
    }
}
