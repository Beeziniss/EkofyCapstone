namespace EkofyApp.Application.Models.Policies;
public sealed record class CreateLegalPolicyRequest
{
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!; // HTML/Markdown
    public bool IsActive { get; set; }
}
