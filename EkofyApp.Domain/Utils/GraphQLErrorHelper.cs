namespace EkofyApp.Domain.Utils;
public static class GraphQLErrorHelper
{
    public static int MapErrorCodeToStatus(string? code)
    {
        // Dùng ToUpperInvariant để tránh case-sensitive
        string normalizedCode = code?.ToUpperInvariant() ?? "UNKNOWN_ERROR";

        return normalizedCode switch
        {
            // Xác thực / Phân quyền
            "AUTH_NOT_AUTHENTICATED" => 401,
            "AUTH_NOT_AUTHORIZED" => 403,
            "TOKEN_EXPIRED" => 401,
            "INVALID_CREDENTIALS" => 401,
            "ACCOUNT_LOCKED" => 403,

            // Lỗi nghiệp vụ
            "VALIDATION_ERROR" => 422,
            "CONFLICT" => 409,
            "NOT_FOUND" => 404,
            "METHOD_NOT_ALLOWED" => 405,
            "RESOURCE_LOCKED" => 423,
            "UNPROCESSABLE_ENTITY" => 422,

            // Lỗi mạng / giới hạn
            "TIMEOUT" => 504,
            "TOO_MANY_REQUESTS" => 429,
            "SERVICE_UNAVAILABLE" => 503,
            "GATEWAY_TIMEOUT" => 504,

            // Lỗi dữ liệu / payload
            "PAYLOAD_TOO_LARGE" => 413,
            "UNSUPPORTED_MEDIA_TYPE" => 415,
            "MALFORMED_REQUEST" => 400,

            // Lỗi hệ thống nội bộ
            "INTERNAL_SERVER_ERROR" => 500,
            "DATABASE_ERROR" => 500,
            "DEPENDENCY_FAILURE" => 502,
            "UNKNOWN_ERROR" => 500,

            // Mặc định
            _ => 999
        };
    }
}
