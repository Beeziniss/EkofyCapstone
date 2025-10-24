namespace EkofyApp.Domain.Utils;
public sealed class HelperCurrencyConverter
{
    // Tỷ giá hiện tại: 1 SGD = 20,263.42 VND
    private const decimal ExchangeRateSgdToVnd = 20263.42m; // chữ 'm' để dùng decimal literal

    public static decimal FormatDecimalLiteral(decimal amount)
    {
        return decimal.Parse($"{amount}m");
    }

    // Chuyển từ SGD sang VND
    public static decimal ConvertSgdToVnd(decimal amountInSgd)
    {
        return amountInSgd * ExchangeRateSgdToVnd;
    }

    // Chuyển từ VND sang SGD
    public static decimal ConvertVndToSgd(decimal amountInVnd)
    {
        return amountInVnd / ExchangeRateSgdToVnd;
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
        string[] zeroDecimalCurrencies = { "vnd", "jpy", "krw", "clp", "pyg" };

        if (zeroDecimalCurrencies.Contains(currency.ToLower()))
        {
            return (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        }
        else
        {
            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }
    }

}
