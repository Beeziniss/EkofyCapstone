using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums
{
    public enum RequestType
    {
        [EnumMember(Value = "PublicRequest")]
        PublicRequest,
        [EnumMember(Value = "DirectRequest")]
        DirectRequest
    }
}
