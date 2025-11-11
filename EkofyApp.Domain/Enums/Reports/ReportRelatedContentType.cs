using System.Runtime.Serialization;

namespace EkofyApp.Domain.Enums.Reports;
public enum ReportRelatedContentType
{
    [EnumMember(Value = "Track")]
    Track,
    [EnumMember(Value = "Artist")]
    Artist,
    [EnumMember(Value = "Listener")]
    Listener,
    [EnumMember(Value = "Comment")]
    Comment,
    [EnumMember(Value = "Request")]
    Request
}
