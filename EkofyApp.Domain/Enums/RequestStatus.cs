using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums
{
    public enum RequestStatus
    {
        [EnumMember(Value = "Blocked")] //khi bị mod ban - CỦA PUBLIC REQUEST
        Blocked,
        [EnumMember(Value = "Closed")] //khi đã hoàn thành hoặc hủy - CỦA PUBLIC REQUEST
        Closed,
        [EnumMember(Value = "Open")] //mặc định là mở - CỦA PUBLIC REQUEST
        Open,
        [EnumMember(Value = "Deleted")]  //chỉ cho xóa khi và chỉ khi đang laf request mở - CỦA PUBLIC REQUEST
        Deleted,
        [EnumMember(Value = "Confirmed")] //- CỦA CẢ DIRECT + PUBLIC REQUEST
        Confirmed,
        [EnumMember(Value = "Rejected")] //- CỦA CẢ DIRECT + PUBLIC REQUEST
        Rejected,
        [EnumMember(Value = "Canceled")] //- CỦA CẢ DIRECT + PUBLIC REQUEST
        Canceled,
        [EnumMember(Value = "Pending")] //- CỦA CẢ DIRECT + PUBLIC REQUEST
        Pending,
    }
}
 