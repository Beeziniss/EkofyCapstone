namespace EkofyApp.Application.Models.Works;
public sealed record class CreateWorkRequest
{
    public string? Description { get; init; }
    public List<CreateWorkSplitRequest> WorkSplits { get; init; } = [];
}
