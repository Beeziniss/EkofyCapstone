using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace EkofyApp.Api.Filters;

public sealed class FluentValidationFilter
{
    public static ValidationProblemDetails ToProblemDetails(ValidationResult validationResult, string instance)
    {
        return new ValidationProblemDetails(validationResult.ToDictionary())
        {
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Type = "Not configured",
            Instance = instance
        };
    }
}
