namespace EkofyApp.Domain.Exceptions;

public class ConflictCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 409; // Default status code for conflict
    public override string ErrorType => "Conflict.htmlx"; // Custom error type for conflict
}
