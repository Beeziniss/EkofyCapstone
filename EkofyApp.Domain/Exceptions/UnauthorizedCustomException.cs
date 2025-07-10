namespace EkofyApp.Domain.Exceptions;

public sealed class UnauthorizedCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 401;
    public override string ErrorType => "Unauthorized.htmlx"; // Custom error type for unauthorized access
}
