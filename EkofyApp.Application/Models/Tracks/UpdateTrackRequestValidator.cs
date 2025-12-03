using FluentValidation;

namespace EkofyApp.Application.Models.Tracks;

public sealed class UpdateTrackRequestValidator : AbstractValidator<UpdateTrackRequest>
{
    public UpdateTrackRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty()
            .WithMessage("Track ID is required.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CategoryIds)
            .Must(categories => categories == null || categories.Count <= 10)
            .WithMessage("Cannot assign more than 10 categories to a track.")
            .When(x => x.CategoryIds != null);

        RuleFor(x => x.CategoryIds)
            .Must(categories => categories == null || categories.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("All category IDs must be valid.")
            .When(x => x.CategoryIds != null);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Count <= 20)
            .WithMessage("Cannot assign more than 20 tags to a track.")
            .When(x => x.Tags != null);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50))
            .WithMessage("All tags must be valid and cannot exceed 50 characters each.")
            .When(x => x.Tags != null);

        // Ensure at least one field is provided for update
        RuleFor(x => x)
            .Must(request => !string.IsNullOrEmpty(request.Description) || 
                           (request.CategoryIds != null && request.CategoryIds.Count > 0) ||
                           (request.Tags != null && request.Tags.Count > 0))
            .WithMessage("At least one field (Description, CategoryIds, or Tags) must be provided for update.");

        RuleFor(x => x.IsPublic)
            .Must(v => v is bool)
            .When(x => x.IsPublic.HasValue)
            .WithMessage("IsPublic must be a boolean value.");
    }
}
