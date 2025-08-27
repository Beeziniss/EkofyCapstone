namespace EkofyApp.Application.Models.Works;
public sealed record class WorkTempRequest
{
    public string Id { get; init; } = null!;
    public string? Description { get; init; }
    public List<CreateWorkSplitRequest> WorkSplits { get; init; } = [];
}
