namespace EkofyApp.Application.Models.Recordings;
public sealed record class RecordingTempRequest
{
    public string Id { get; init; } = null!;
    public string? Description { get; init; } = null!;
    public List<CreateRecordingSplitRequest> RecordingSplitRequests { get; init; } = [];
}
