using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums;
public enum PaymentMethodType
{
    [EnumMember(Value = "card")]
    Card,
    [EnumMember(Value = "link")]
    Link
}
