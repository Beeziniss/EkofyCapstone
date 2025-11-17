using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums
{
    public enum ArtistPackageStatus
    {
        [EnumMember(Value = "Enabled")]
        Enabled,
        [EnumMember(Value = "Disabled")]
        Disabled,
        //[EnumMember(Value = "Pending")]
        //Pending,
        //[EnumMember(Value = "Rejected")]
        //Rejected
    }
}
