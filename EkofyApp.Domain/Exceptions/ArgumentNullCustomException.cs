namespace EkofyApp.Domain.Exceptions;
public sealed class ArgumentNullCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 400; // Default status code is 400
    public override string ErrorType => "BadRequest.htmlx"; // Custom error type for bad requests
}
