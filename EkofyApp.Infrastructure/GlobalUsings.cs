global using PortalSession = Stripe.BillingPortal.Session;
global using StripeInvoice = Stripe.Invoice;
global using StripeSubscription = Stripe.Subscription;
global using Subscription = EkofyApp.Domain.Entities.Subscription;
global using CheckoutOption = Stripe.Checkout;
global using BillingPortalOption = Stripe.BillingPortal;
global using StripeCoupon = Stripe.Coupon;
global using EntityCoupon = EkofyApp.Domain.Entities.Coupon;

global using FFmpegNative = Xabe.FFmpeg;

namespace EkofyApp.Infrastructure;
