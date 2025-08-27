using FluentValidation;

namespace EkofyApp.Application.Models.Recordings;
public sealed class CreateRecordingRequestValidator : AbstractValidator<CreateRecordingRequest>
{
    public CreateRecordingRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 5000 characters.");

        RuleFor(x => x.RecordingSplits)
            .ForEach(x => x.SetValidator(new CreateRecordingSplitRequestValidator()));
    }
}
