namespace EkofyApp.Domain.Exceptions;

public sealed class UnprocessableEntityCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 422; // Default status code for unprocessable entity
    public override string ErrorType => "UnprocessableEntity.htmlx"; // Custom error type for unprocessable entity
}
