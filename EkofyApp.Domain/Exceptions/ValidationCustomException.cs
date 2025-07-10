namespace EkofyApp.Domain.Exceptions;

public sealed class ValidationCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 400; // Default status code is 400
    public override string ErrorType => "ValidationError.htmlx"; // Custom error type for validation errors
}
