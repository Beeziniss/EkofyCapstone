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
        Log.Fatal(error.Exception, error.Exception?.Message ?? "Can not get Message");

        return error
            .WithMessage("Đã xảy ra lỗi không xác định. Lỗi hệ thống")
            .WithCode("UNHANDLED_ERROR")
            .SetExtension("status", 500);
    }
}
