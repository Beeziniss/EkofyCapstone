using EkofyApp.Domain.Base;

namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Review : TimeStamped
{
    public int Rating { get; set; }
    public string Content { get; set; } = null!;
}
