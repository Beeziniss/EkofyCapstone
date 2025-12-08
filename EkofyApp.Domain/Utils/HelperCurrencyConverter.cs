namespace EkofyApp.Domain.Utils;
public sealed class HelperCurrencyConverter
{
    // Tỷ giá hiện tại: 1 SGD = 20,500.00 VND
    private const decimal ExchangeRateSgdToVnd = 20500.00m; // chữ 'm' để dùng decimal literal

    // Tỷ giá hiện tại: 1 USD = 27,000.00 VND
    private const decimal ExchangeRateUsdToVnd = 27000.00m; // chữ 'm' để dùng decimal literal

    public static decimal FormatDecimalLiteral(decimal amount)
    {
        return decimal.Parse($"{amount}m");
    }

    // Chuyển từ SGD sang VND
    public static decimal ConvertSgdCentsDecimalToVndDecimal(long sgdAmountInCents)
    {
        decimal sgd = sgdAmountInCents / 100m; // convert cents → SGD
        return sgd * ExchangeRateSgdToVnd;
    }

    public static decimal ConvertStripeAmountToDecimal(long amount, string currency)
    {
        // Các loại tiền không có đơn vị lẻ
        string[] zeroDecimalCurrencies = ["vnd", "jpy", "krw", "clp", "pyg"];

        if (zeroDecimalCurrencies.Contains(currency.ToLower()))
        {
            return amount;
        }
        else
        {
            return amount / 100m;
        }
    }

    public static long ConvertDecimalToStripeAmount(decimal amount, string currency)
    {
        string[] zeroDecimalCurrencies = ["vnd", "jpy", "krw", "clp", "pyg"];

        if (zeroDecimalCurrencies.Contains(currency.ToLower()))
        {
            return (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        }
        else
        {
            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }
    }

    #region SGD <-> VND
    // Chuyển từ VND sang SGD
    public static decimal ConvertVndToSgd(decimal amountInVnd)
    {
        return amountInVnd / ExchangeRateSgdToVnd;
    }

    public static long ConvertVndDecimalToStripeAmountSgdLong(decimal amount)
    {
        decimal sgdAmount = ConvertVndToSgd(amount);
        return ConvertDecimalToStripeAmount(sgdAmount, "sgd");
    }
    #endregion

    #region USD <-> VND
    // Chuyển từ VND sang USD
    public static decimal ConvertVndToUsd(decimal amountInVnd)
    {
        return amountInVnd / ExchangeRateUsdToVnd;
    }

    public static long ConvertVndDecimalToStripeAmountUsdLong(decimal amountInVnd)
    {
        decimal usdAmount = ConvertVndToUsd(amountInVnd);
        return ConvertDecimalToStripeAmount(usdAmount, "usd");
    }
    #endregion
}
