namespace EkofyApp.Domain.Utils;
public class HelperMethod
{
    public static IEnumerable<long> GetValidBitrates()
    {
        // Đơn vị kbps -> 128000 tương đương 128 kbps

        // Dùng cho convert to HLS
        IEnumerable<long> validBitrates = [128000, 256000, 320000];

        return validBitrates;
    }
}
