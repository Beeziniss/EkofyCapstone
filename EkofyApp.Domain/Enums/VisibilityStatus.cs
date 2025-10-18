using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum VisibilityStatus
{
    [EnumMember(Value = "Public")]
    Public,
    [EnumMember(Value = "Private")]
    Private,
}
