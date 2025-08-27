namespace EkofyApp.Application.Models.Recordings;
public sealed record class CreateRecordingRequest
{
    public string? Description { get; init; }
    public List<CreateRecordingSplitRequest> RecordingSplits { get; init; } = [];
}
