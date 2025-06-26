using EkofyApp.Application.Models.Payment.Momo;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Momo;

namespace EkofyApp.Api.GraphQL.Mutation.Payment.Momo;

[ExtendObjectType(typeof(MutationInitialization))]
[MutationType]
public class MomoMutation(IMomoService momoService)
{
    private readonly IMomoService _momoService = momoService;

    public async Task<MomoPaymentResponse> CreateMomoPaymentAsync(CreateMomoPaymentRequest createMomoPaymentRequest)
    {
        return await _momoService.CreatePaymentQRLinkAsync(createMomoPaymentRequest);
    }

    public async Task<MomoPaymentResponse> CreateMomoPaymentVisaAsync(CreateMomoPaymentRequest createMomoPaymentRequest)
    {
        return await _momoService.CreatePaymentLinkVisaAsync(createMomoPaymentRequest);
    }
}
