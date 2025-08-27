using EkofyApp.Domain.Utils;
using FluentValidation;

namespace EkofyApp.Application.Models.Tracks;
public sealed class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Track name is required.")
            .MaximumLength(200).WithMessage("Track name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.MainArtistIds)
            //.NotEmpty().WithMessage("At least one main artist ID is required.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Main artist IDs must be unique.");

        RuleFor(x => x.FeaturedArtistIds)
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Featured artist IDs must be unique.");

        RuleFor(x => x.CoverImage)
            .NotEmpty().WithMessage("Cover image URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Cover image must be a valid URL.");

        RuleFor(x => x.PreviewVideo)
            //.NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.PreviewVideo)).WithMessage("Preview video URL cannot be empty if provided.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).When(x => !string.IsNullOrWhiteSpace(x.PreviewVideo)).WithMessage("Preview video must be a valid URL.");

        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("At least one category ID is required.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Category IDs must be unique.");

        RuleFor(x => x.Tags)
            .ForEach(tag => tag.MaximumLength(50).WithMessage("Each tag must not exceed 50 characters."));

        RuleFor(x => x.IsExplicit)
            .NotNull().WithMessage("Explicit content flag is required.");

        RuleFor(x => x.Lyrics)
            .MaximumLength(5000).WithMessage("Lyrics must not exceed 5000 characters.");

        RuleFor(x => x.IsReleased)
            .NotNull().WithMessage("Release status is required.");

        RuleFor(x => x.ReleaseDate)
            .GreaterThanOrEqualTo(HelperMethod.GetUtcPlus7TimeOffset()).When(x => x.IsReleased)
            .WithMessage("Release date must be in the present or future if the track is marked as released.");

        RuleFor(x => x.ReleaseStatus)
            .IsInEnum().WithMessage("Release status must be a valid enum value.");

        RuleFor(x => x.IsOriginal)
            .NotNull().WithMessage("Original content flag is required.");
    }
}
