namespace EkofyApp.Domain.Exceptions
{
    public class NotFoundCustomException : BaseException
    {
        public override int StatusCode => 404;
        public override string ErrorType => "NotFound.htmlx";
        public NotFoundCustomException(string message) : base(message) // Default status code is 404
        {
        }
    }
}
