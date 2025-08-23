using EkofyApp.Domain.EmbeddedDocuments;

namespace EkofyApp.Domain.Entities;
public sealed class Recording : IEntityCustom
{
    public string Id { get; set; } = null!; // Unique identifier for the recording
    //public string ISRC { get; set; } = null!; // International Standard Recording Code (ISRC) for the recording
    public string? Description { get; set; } // Description of the recording, if available
    public List<RecordingSplit> RecordingSplits { get; set; } = []; // List of splits for the recording, e.g., 50% to Artist A, 50% to Artist B
}
