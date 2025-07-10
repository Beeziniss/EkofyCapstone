using EkofyApp.Application.Models.Payment.Momo;
using EkofyApp.Application.ThirdPartyServiceInterfaces.Payment.Momo;
using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Settings.Momo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Refit;
using System.Security.Cryptography;
using System.Text;

namespace EkofyApp.Infrastructure.ThirdPartyServices.Payment.Momo;
public sealed class MomoService(IMomoApi momoApi, MomoSetting momoSetting, ILogger<MomoService> logger) : IMomoService
{
    private readonly IMomoApi _momoApi = momoApi;
    private readonly MomoSetting _momoSetting = momoSetting;
    private readonly ILogger<MomoService> _logger = logger;

    public async Task<MomoPaymentResponse> CreatePaymentQRLinkAsync(CreateMomoPaymentRequest createMomoPaymentRequest)
    {
        _logger.LogInformation("Creating Momo payment link with request: {@Request}", createMomoPaymentRequest);

        string requestId = ObjectId.GenerateNewId().ToString();
        string extraData = "";
        string accessKey = _momoSetting.AccessKey;
        string secretKey = _momoSetting.SecretKey;
        string partnerCode = _momoSetting.PartnerCode;
        string returnUrl = _momoSetting.ReturnUrl;
        string notifyUrl = _momoSetting.NotifyUrl;
        string requestTypeQR = _momoSetting.RequestTypeQR;

        // Tránh bị trùng ID đơn hàng
        string orderIdObjectId = ObjectId.GenerateNewId().ToString();

        string rawHash = $"partnerCode={partnerCode}&accessKey={accessKey}&requestId={requestId}&amount={createMomoPaymentRequest.Amount}&orderId={orderIdObjectId}&orderInfo={createMomoPaymentRequest.OrderInfo}&returnUrl={returnUrl}&notifyUrl={notifyUrl}&extraData=";

        string signature = CreateSignature(rawHash, secretKey);

        MomoPaymentRequest request = new()
        {
            PartnerCode = partnerCode,
            AccessKey = accessKey,
            RequestId = requestId,
            Amount = createMomoPaymentRequest.Amount.ToString(),
            OrderId = orderIdObjectId,
            OrderInfo = createMomoPaymentRequest.OrderInfo,
            ReturnUrl = returnUrl,
            NotifyUrl = notifyUrl,
            ExtraData = extraData,
            Lang = "vi",
            RequestType = requestTypeQR,
            Signature = signature
        };

        ApiResponse<MomoPaymentResponse> response = await _momoApi.PostPaymentQRAsync(request);

        _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);
        //_logger.Log(LogLevel.Information, "Parsed Momo response: {@Content}", response.Content);
        //_logger.LogInformation("Raw Response: {@Response}", response);

        MomoPaymentResponse content = response.Content ?? throw new ArgumentNullCustomException("Response is null");

        _logger.Log(LogLevel.Information, "Parsed Momo response: {@Content}", content);

        return content;
    }

    public async Task<MomoPaymentResponse> CreatePaymentLinkVisaAsync(CreateMomoPaymentRequest createMomoPaymentRequest)
    {
        _logger.LogInformation("Creating Momo payment link with request: {@Request}", createMomoPaymentRequest);

        string requestId = ObjectId.GenerateNewId().ToString();
        string extraData = "";
        string accessKey = _momoSetting.AccessKey;
        string secretKey = _momoSetting.SecretKey;
        string partnerCode = _momoSetting.PartnerCode;
        string returnUrl = _momoSetting.ReturnUrl;
        string requestTypeVisa = _momoSetting.RequestTypeVisa;

        // Tránh bị trùng ID đơn hàng
        string orderIdObjectId = ObjectId.GenerateNewId().ToString();

        // Tạo chuỗi rawHash để tạo chữ ký
        string rawHash = $"accessKey={accessKey}&amount={createMomoPaymentRequest.Amount}&extraData=&ipnUrl={returnUrl}&orderId={orderIdObjectId}&orderInfo={createMomoPaymentRequest.OrderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestTypeQR={requestTypeVisa}";

        // Tạo chữ ký
        string signature = CreateSignature(rawHash, secretKey);

        #region HttpClient
        //var paymentRequest = new
        //{
        //    partnerCode,
        //    accessKey,
        //    requestId,
        //    amount = amountS,
        //    orderId = orderIdS,
        //    orderInfo = createMomoPaymentRequest.OrderInfo,
        //    returnUrl,
        //    notifyUrl,
        //    //ipnUrl,
        //    extraData,
        //    lang = "vi",
        //    requestTypeQR,
        //    signature
        //};

        //string jsonPaymentRequest = JsonConvert.SerializeObject(paymentRequest);
        //Console.WriteLine("JSON Request: " + jsonPaymentRequest);

        //using (var client = new HttpClient())
        //{
        //    var content = new StringContent(jsonPaymentRequest, Encoding.UTF8, "application/json");
        //    var response = await client.PostAsync("https://test-payment.momo.vn/gw_payment/transactionProcessor", content);
        //    var responseString = await response.Content.ReadAsStringAsync();
        //    Console.WriteLine("JSON Response: " + responseString);

        //    var responseObject = JsonConvert.DeserializeObject<MomoPaymentResponse>(responseString);
        //    if (responseObject != null && !string.IsNullOrEmpty(responseObject.PayUrl))
        //    {
        //        //var paymentURL = responseObject.payUrl;
        //        return responseObject;
        //    }
        //    else
        //    {
        //        Console.WriteLine("Fail from MOMO Payment");
        //        // Xử lý lỗi
        //        return responseObject;
        //    }
        //}
        #endregion

        #region Refit
        MomoPaymentRequest request = new()
        {
            PartnerCode = partnerCode,
            AccessKey = accessKey,
            RequestId = requestId,
            Amount = createMomoPaymentRequest.Amount.ToString(),
            OrderId = orderIdObjectId,
            OrderInfo = createMomoPaymentRequest.OrderInfo,
            RedirectUrl = returnUrl,
            IpnUrl = returnUrl,
            ExtraData = extraData,
            Lang = "vi",
            RequestType = requestTypeVisa,
            Signature = signature
        };

        // Gọi API Momo để tạo link thanh toán
        ApiResponse<MomoPaymentResponse> response = await _momoApi.PostPaymentVisaAsync(request);

        _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);

        // Lấy content từ response
        MomoPaymentResponse content = response.Content ?? throw new ArgumentNullCustomException("Response is null");

        _logger.Log(LogLevel.Information, "Parsed Momo response: {@Content}", content);

        return content;
        #endregion
    }

    private static string CreateSignature(string rawData, string secretKey)
    {
        using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(secretKey));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
