using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/webhook/stripe")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] //"Bearer"
public sealed class StripeController(IStripeService stripeService) : ControllerBase
{
    private readonly IStripeService _stripeService = stripeService;

    [HttpPost("customers")]
    public async Task<IActionResult> HandleWebhookCustomerAsync()
    {
        string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        string? stripeSignature = Request.Headers["Stripe-Signature"];
        if(string.IsNullOrEmpty(stripeSignature))
        {
            return BadRequest("Missing Stripe-Signature header");
        }

        _stripeService.HandleWebhookCustomer(json, stripeSignature);

        return Ok("StripeController is working!");
    }

    [HttpPost("/v1/accounts")]
    public async Task<IActionResult> HandleWebhookAccountAsync()
    {
        string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        string? stripeSignature = Request.Headers["Stripe-Signature"];
        if (string.IsNullOrEmpty(stripeSignature))
        {
            return BadRequest("Missing Stripe-Signature header");
        }

        _stripeService.HandleWebhookExpressConnectedAccount(json, stripeSignature);
        return Ok("StripeController is working!");
    }

    #region Test
    [HttpPost("connected-account")]
    public async Task<IActionResult> CreateExpressConnectedAccountAsync()
    {
        var account = await _stripeService.CreateExpressConnectedAccountTest();
        return Ok(account);
    }

    [HttpPost("onboarding")]
    public IActionResult CreateAccountOnboardingLink([FromQuery] string refreshUrl, [FromQuery] string returnUrl)
    {
        var accountLink = _stripeService.CreateAccountOnboardingLinkTest(refreshUrl, returnUrl);
        return Ok(accountLink);
    }
    #endregion
}
