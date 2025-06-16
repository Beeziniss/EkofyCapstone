using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using EkofyApp.Domain.Exceptions;
using Serilog;

namespace EkofyApp.Api.Filters
{
    public class BaseExceptionFilter : IExceptionFilter
    {
        public BaseExceptionFilter()
        {
        }

        public void OnException(ExceptionContext context)
        {
            ProblemDetails problem;
            Exception exception = context.Exception;

            if (context.Exception is BaseException appEx)
            {
                //_logger.LogWarning(appEx, "=============================================================\nSystem error occurred at UTC+7 time: {Time}", TimeControl.GetUtcPlus7Time());

                Log.Error(exception, exception.Message);
                problem = new ProblemDetails
                {
                    Title = ReasonPhrases.GetReasonPhrase(appEx.StatusCode),
                    Status = appEx.StatusCode,
                    Detail = appEx.Message,
                    Type = appEx.ErrorType,
                    Instance = context.HttpContext.Request.Path
                };

                context.HttpContext.Response.StatusCode = appEx.StatusCode;
            }
            else
            {
                //_logger.LogError(context.Exception, "=============================================================\nSystem error occurred at UTC+7 time: {Time}", TimeControl.GetUtcPlus7Time());

                Log.Fatal(exception, exception.Message);
                problem = new ProblemDetails
                {
                    Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError), // Default to 500 Internal Server Error
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = exception.Message,
                    Type = "Lỗi này do chưa config hoặc chưa xác định được"
                };

                context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            context.ExceptionHandled = true;
        }
    }

}
