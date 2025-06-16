namespace EkofyApp.Domain.Exceptions
{
    public class UnauthorizedCustomException : BaseException
    {
        public override int StatusCode => 401;
        public override string ErrorType => "Unauthorized.htmlx"; // Custom error type for unauthorized access
        public UnauthorizedCustomException(string message) : base(message) // Default status code is 401
        {
        }
    }
}
