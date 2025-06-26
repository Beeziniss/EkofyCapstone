
using EkofyApp.Application.Models.Payment.Momo;

namespace EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Momo;
public interface IMomoService
{
    Task<MomoPaymentResponse> CreatePaymentQRLinkAsync(CreateMomoPaymentRequest createMomoPaymentRequest);
    Task<MomoPaymentResponse> CreatePaymentLinkVisaAsync(CreateMomoPaymentRequest createMomoPaymentRequest);
}
