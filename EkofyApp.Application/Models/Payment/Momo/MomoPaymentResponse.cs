using System.Text.Json.Serialization;

namespace EkofyApp.Application.Models.Payment.Momo;
public class MomoPaymentResponse
{
    public string? PartnerCode { get; set; }
    public string? RequestId { get; set; }
    public string? OrderId { get; set; }
    public long? Amount { get; set; }
    public long? ResponseTime { get; set; }

    public string? Message { get; set; }
    public string? LocalMessage { get; set; }

    public int? ResultCode { get; set; }

    public string? PayUrl { get; set; }
}
