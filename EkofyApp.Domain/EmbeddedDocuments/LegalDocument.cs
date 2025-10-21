using EkofyApp.Domain.Base;
using EkofyApp.Domain.Enums;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class LegalDocument
{
    public string Name { get; set; } = null!; // Title of the document, e.g., "Artist Agreement"
    public string DocumentUrl { get; set; } = null!; // URL to the document, e.g., "https://example.com/document.pdf"
    public DocumentType DocumentType { get; set; } // Type of the document, e.g., "Contract", "Agreement", etc.
    public string Note { get; set; } = null!; // Note of the document, e.g., text of the agreement
}
