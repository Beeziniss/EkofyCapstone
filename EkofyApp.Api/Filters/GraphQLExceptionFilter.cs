using EkofyApp.Domain.Exceptions;
using Serilog;

namespace EkofyApp.Api.Filters;

public sealed class GraphQLExceptionFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is BaseException baseException)
        {
            Log.Error(baseException, baseException.Message);

            return error
                .WithMessage(baseException.Message)
                .WithCode($"{baseException.GetType().Name}")
                .SetExtension("status", baseException.StatusCode);
                //.SetExtension("type", baseException.ErrorType);
        }

        // Handle specific exceptions that are not BaseException
        if (error.Exception is null)
        {
            string code = error.Code ?? "BAD_REQUEST";

            int status = code switch
            {
                "AUTH_NOT_AUTHENTICATED" => 401, // chưa đăng nhập / token invalid
                "AUTH_NOT_AUTHORIZED" => 403, // không đủ quyền/role
                "HC0015" or "HC0016" or "HC0017" => 400, // ví dụ các mã validation (tùy version)
                _ => 400
            };

            Log.Error("GraphQL Error: {Message}", error.Message);

            // Giữ nguyên thông điệp gốc để client hiểu đúng ngữ cảnh
            return error
                .WithMessage("Error from GraphQL Server")
                .WithCode(code)
                .SetExtension("status", status)
                .SetExtension("detail", error.Message);
        }

        // Unhandled exception fallback
        string detail = error.Exception?.Message ?? "GraphQL is misconfigured.";
        string fallbackMessage = "An unknown error has occurred. System error.";

        Log.Fatal(error.Exception, detail);

        return error
            .WithMessage(fallbackMessage)
            .WithCode("UNHANDLED_ERROR")
            .SetExtension("status", 500)
            .SetExtension("detail", detail);
    }
}
