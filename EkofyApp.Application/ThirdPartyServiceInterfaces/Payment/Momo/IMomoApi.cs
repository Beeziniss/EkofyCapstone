using EkofyApp.Application.Models.Payment.Momo;
using Refit;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Momo;
public interface IMomoApi
{
    const string QRCode = "gw_payment/transactionProcessor";
    const string VISA = "v2/gateway/api/create";

    [Post($"/{QRCode}")]
    Task<ApiResponse<MomoPaymentResponse>> PostPaymentQRAsync([Body] MomoPaymentRequest request);

    [Post($"/{VISA}")]
    Task<ApiResponse<MomoPaymentResponse>> PostPaymentVisaAsync([Body] MomoPaymentRequest request);
}
