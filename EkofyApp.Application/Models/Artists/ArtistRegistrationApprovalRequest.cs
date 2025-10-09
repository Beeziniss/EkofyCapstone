using EkofyApp.Domain.Enums.Users;

namespace EkofyApp.Application.Models.Artists;

public sealed record class ArtistRegistrationApprovalRequest
{
    public string UserId { get; init; } = null!; // Artist Registration ID trong Redis
    public string Email { get; init; } = null!; // Email của artist
    public string FullName { get; init; } = null!; // Tên đầy đủ của artist
    public string? RejectionReason { get; init; }
}