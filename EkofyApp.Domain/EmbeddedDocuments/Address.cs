namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Address
{
    public string? Street { get; set; }
    public string? Ward { get; set; }
    public string? Province { get; set; }

    public string? OldDistrict { get; set; } // Quận/Huyện
    public string? OldWard { get; set; } // Phường/Xã
    public string? OldProvince { get; set; } // Tỉnh/Thành phố

    public string? AddressLine { get; set; } // Địa chỉ đầy đủ, bao gồm số nhà, tên đường, phường/xã, quận/huyện, thành phố/tỉnh
}
