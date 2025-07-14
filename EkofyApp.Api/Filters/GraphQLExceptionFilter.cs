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
