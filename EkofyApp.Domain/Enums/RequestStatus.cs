using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums
{
    public enum RequestStatus
    {
        [EnumMember(Value = "Blocked")]
        Blocked,
        [EnumMember(Value = "Closed")]
        Closed,
        [EnumMember(Value = "Open")]
        Open,
        [EnumMember(Value = "Deleted")]
        Deleted
    }
}
