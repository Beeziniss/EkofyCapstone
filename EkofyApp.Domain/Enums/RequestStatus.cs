using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums
{
    public enum RequestStatus
    {
        [EnumMember(Value = "Blocked")] //khi bị mod ban
        Blocked,
        [EnumMember(Value = "Closed")] //khi đã hoàn thành hoặc hủy
        Closed,
        [EnumMember(Value = "Open")] //mặc định là mở
        Open,
        [EnumMember(Value = "Deleted")]  //chỉ cho xóa khi và chỉ khi đang laf request mở
        Deleted,
    }
}
 