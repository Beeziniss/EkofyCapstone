namespace EkofyApp.Domain.EmbeddedDocuments;
public sealed class Address
{
    public string? Street { get; set; }
    public string? Ward { get; set; }
    public string? City { get; set; }

    public string? AddressLine { get; set; } // Địa chỉ đầy đủ, bao gồm số nhà, tên đường, phường/xã, quận/huyện, thành phố/tỉnh
}
