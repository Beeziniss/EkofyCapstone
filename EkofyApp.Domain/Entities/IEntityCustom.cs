namespace EkofyApp.Domain.Entities;

public interface IEntityCustom
{
    // Có thể dùng TDataType để làm generic
    // Ví dụ: TDataType có thể là ObjectId, string, Guid, v.v.
    // Vì conventions thường dùng trong server là string
    public string Id { get; set; }
}
