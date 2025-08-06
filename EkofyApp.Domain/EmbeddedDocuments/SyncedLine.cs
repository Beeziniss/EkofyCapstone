namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class SyncedLine
{
    public string Text { get; set; } = null!; // The text of the synced line
    public double Time { get; set; } = default; // The time in seconds when this line should be displayed
}
