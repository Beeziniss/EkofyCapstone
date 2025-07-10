namespace EkofyApp.Domain.Exceptions;

public sealed class ExternalServiceCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 503; // Default status code for external service errors
}
