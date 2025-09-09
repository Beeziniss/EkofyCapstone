using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.BillingPortalConfig;
public enum CustomerUpdate
{
    [EnumMember(Value = "address")]
    Address,
    [EnumMember(Value = "email")]
    Email,
    [EnumMember(Value = "phone")]
    Phone,
    [EnumMember(Value = "shipping")]
    Shipping,
    [EnumMember(Value = "tax_id")]
    TaxId
}
