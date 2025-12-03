using EkofyApp.Domain.EmbeddedDocuments;
using FluentValidation;

namespace EkofyApp.Application.Models.LegalDocs;
public sealed class LegalDocumentValidator : AbstractValidator<LegalDocument>
{
    public LegalDocumentValidator()
    {
        RuleFor(d => d.DocumentType)
            .IsInEnum().WithMessage("Each legal document must have a valid document type.");

        RuleFor(d => d.DocumentUrl)
            .NotEmpty().WithMessage("Each legal document must have a URL.");
            //.Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            //.WithMessage("Each legal document URL must be valid.");
    }
}
