using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum ArtistPackageOrderStatus
{
    [EnumMember(Value = "pending")]
    Pending,
    [EnumMember(Value = "in_progress")]
    InProgress,
    [EnumMember(Value = "submitted_for_review")]
    SubmittedForReview,
    [EnumMember(Value = "revision_requested")]
    RevisionRequested,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "cancelled")]
    Cancelled,
    [EnumMember(Value = "disputed")]
    Disputed
}