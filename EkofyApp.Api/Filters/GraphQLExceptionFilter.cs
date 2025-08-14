using EkofyApp.Domain.Exceptions;
using EkofyApp.Domain.Utils;
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
            string code = error.Code ?? "UNKNOWN_ERROR";

            int status = GraphQLErrorHelper.MapErrorCodeToStatus(code);

            Log.Fatal("GraphQL Error: {Message}", error.Message);

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
