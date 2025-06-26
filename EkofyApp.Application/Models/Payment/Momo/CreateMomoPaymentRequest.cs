namespace EkofyApp.Application.Models.Payment.Momo;
public class CreateMomoPaymentRequest
{
    public long Amount { get; set; }
    public string OrderId { get; set; }
    public string OrderInfo { get; set; }
}
