using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum HistoryActionType
{
    [EnumMember(Value = "Approved")]
    Approved,
    [EnumMember(Value = "Rejected")]
    Rejected,
    [EnumMember(Value = "Canceled")]
    Canceled,
    [EnumMember(Value = "RequestChange")]
    RequestChange,
    [EnumMember(Value = "Dismissed")]
    Dismissed
}
