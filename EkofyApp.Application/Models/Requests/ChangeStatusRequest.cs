using EkofyApp.Domain.Enums;

namespace EkofyApp.Application.Models.Requests
{
    public sealed record ChangeStatusRequest
    {
        public string RequestId { get; set; } = default!;
        public RequestStatus Status { get; set; }
    }
}
