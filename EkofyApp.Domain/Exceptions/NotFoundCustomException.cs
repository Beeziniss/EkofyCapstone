namespace EkofyApp.Domain.Exceptions;

public sealed class NotFoundCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 404;
    public override string ErrorType => "NotFound.htmlx";
}
