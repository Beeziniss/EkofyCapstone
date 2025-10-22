using EkofyApp.Application.Models.LegalDocs;
using EkofyApp.Domain.Enums;
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

        //RuleFor(x => x.IsReleased)
        //    .NotNull().WithMessage("Release status is required.");

        //RuleFor(x => x.ReleaseDate)
        //    .GreaterThanOrEqualTo(_ => HelperMethod.GetUtcPlus7TimeOffset().AddDays(3).AddHours(2)).When(x => x.IsReleased)
        //    .WithMessage("Release date must be in the present or future if the track is marked as released.");

        //RuleFor(x => x.ReleaseStatus)
        //    .IsInEnum().WithMessage("Release status must be a valid enum value.");

        RuleFor(x => x)
            .Custom((model, context) =>
            {
                DateTimeOffset now = HelperMethod.GetUtcPlus7TimeOffset();

                switch (model.ReleaseStatus)
                {
                    case ReleaseStatus.Official:
                        if (!model.IsReleased)
                        {
                            context.AddFailure(nameof(model.IsReleased), "IsReleased must be true when ReleaseStatus is Official.");
                        }
                        if (model.ReleaseDate != null)
                        {
                            context.AddFailure(nameof(model.ReleaseDate), "ReleaseDate must be null when ReleaseStatus is Official.");
                        }
                        break;

                    case ReleaseStatus.NotAnnounced:
                        if (model.IsReleased)
                        {
                            context.AddFailure(nameof(model.IsReleased), "IsReleased must be false when ReleaseStatus is Not Announced.");
                        }
                        if (model.ReleaseDate == null)
                        {
                            context.AddFailure(nameof(model.ReleaseDate), "ReleaseDate is required when ReleaseStatus is Not Announced.");
                        }
                        break;

                    case ReleaseStatus.Delayed:
                    case ReleaseStatus.Canceled:
                    case ReleaseStatus.Leaked:
                        // Tuỳ vào logic nghiệp vụ muốn:
                        // Ví dụ giả định muốn:
                        // - IsReleased = false
                        // - ReleaseDate = null
                        if (model.IsReleased)
                        {
                            context.AddFailure(nameof(model.IsReleased), $"IsReleased must be false when ReleaseStatus is {model.ReleaseStatus}.");
                        }
                        if (model.ReleaseDate != null)
                        {
                            context.AddFailure(nameof(model.ReleaseDate), $"ReleaseDate must be null when ReleaseStatus is {model.ReleaseStatus}.");
                        }
                        break;
                }

                // Thêm điều kiện riêng cho ngày phát hành nếu đã phát hành
                if (model.IsReleased && model.ReleaseStatus != ReleaseStatus.Official)
                {
                    if (model.ReleaseDate == null)
                    {
                        context.AddFailure(nameof(model.ReleaseDate), "ReleaseDate is required if the track is marked as released (except Official).");
                    }
                    else if (model.ReleaseDate < now.AddDays(3).AddHours(2))
                    {
                        context.AddFailure(nameof(model.ReleaseDate), "ReleaseDate must be at least 3 days and 2 hours from now.");
                    }
                }
            });

        RuleFor(x => x.LegalDocuments)
            .NotEmpty().WithMessage("At least one legal document is required.")
            .ForEach(doc => doc.SetValidator(new LegalDocumentValidator()));

        RuleFor(x => x.IsOriginal)
            .NotNull().WithMessage("Original content flag is required.");
    }
}
