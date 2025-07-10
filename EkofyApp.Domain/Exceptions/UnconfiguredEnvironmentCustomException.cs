namespace EkofyApp.Domain.Exceptions;
public class UnconfiguredEnvironmentCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 504; // Default status code for server errors
    public override string ErrorType => "UnconfiguredEnvironmentCustomException.htmlx"; // Custom error type for not configured environment
}
