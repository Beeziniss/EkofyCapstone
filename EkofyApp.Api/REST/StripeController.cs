using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Stripe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.REST;
[Route("api/webhook/stripe")]
[ApiController]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] //"Bearer"
public sealed class StripeController(IStripeService stripeService, IStripeWebhookService stripeWebhookService) : ControllerBase
{
    private readonly IStripeService _stripeService = stripeService;
    private readonly IStripeWebhookService _stripeWebhookService = stripeWebhookService;

    [AllowAnonymous, HttpPost("customers")]
    public async Task<IActionResult> HandleWebhookCustomerAsync()
    {
        try
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            string? stripeSignature = Request.Headers["Stripe-Signature"];
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe-Signature header");
            }

            await _stripeWebhookService.HandleWebhookCustomerAsync(json, stripeSignature);

            return Ok("Customer webhook processed successfully!");
        }
        catch (Exception)
        {
            // Log lỗi nhưng trả về status code 2xx để ngăn Stripe retry
            // theo khuyến nghị của Stripe cho việc xử lý webhook
            return Ok("Webhook received but processing failed - will not retry");
        }
    }

    [AllowAnonymous, HttpPost("v1/accounts")]
    public async Task<IActionResult> HandleWebhookAccountAsync()
    {
        try
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            string? stripeSignature = Request.Headers["Stripe-Signature"];
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe-Signature header");
            }

            _stripeWebhookService.HandleWebhookExpressConnectedAccount(json, stripeSignature);
            return Ok("Account webhook processed successfully!");
        }
        catch (Exception)
        {
            return Ok("Webhook received but processing failed - will not retry");
        }
    }

    [AllowAnonymous, HttpPost("checkout-session")]
    public async Task<IActionResult> HandleWebhookCheckoutSessionAsync()
    {
        try
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string? stripeSignature = Request.Headers["Stripe-Signature"];
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe-Signature header");
            }

            await _stripeWebhookService.HandleWebhookCheckoutSessionAsync(json, stripeSignature);
            return Ok("Checkout session webhook processed successfully!");
        }
        catch (Exception)
        {
            return Ok("Webhook received but processing failed - will not retry");
        }
    }

    [AllowAnonymous, HttpPost("invoice")]
    public async Task<IActionResult> HandleWebhookInvoiceAsync()
    {
        try
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string? stripeSignature = Request.Headers["Stripe-Signature"];
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe-Signature header");
            }
            
            await _stripeWebhookService.HandleWebhookInvoiceAsync(json, stripeSignature);
            return Ok("Invoice webhook processed successfully!");
        }
        catch (Exception)
        {
            return Ok("Webhook received but processing failed - will not retry");
        }
    }

    [AllowAnonymous, HttpPost("payout")]
    public async Task<IActionResult> HandleWebhookPayoutAsync()
    {
        try
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string? stripeSignature = Request.Headers["Stripe-Signature"];
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe-Signature header");
            }

            await _stripeWebhookService.HandleWebhookPayoutAsync(json, stripeSignature);
            return Ok("Payout webhook processed successfully!");
        }
        catch (Exception)
        {
            return Ok("Webhook received but processing failed - will not retry");
        }
    }

    [AllowAnonymous, HttpPost("refund")]
    public async Task<IActionResult> HandleWebhookRefundAsync()
    {
        try
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string? stripeSignature = Request.Headers["Stripe-Signature"];
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return BadRequest("Missing Stripe-Signature header");
            }

            await _stripeWebhookService.HandleWebhookRefundAsync(json, stripeSignature);
            return Ok("Refund webhook processed successfully!");
        }
        catch (Exception)
        {
            return Ok("Webhook received but processing failed - will not retry");
        }
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
