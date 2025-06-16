namespace EkofyApp.Domain.Exceptions
{
    public class ForbiddenCustomException : BaseException
    {
        public override int StatusCode => 403; // Default status code for forbidden access
        public override string ErrorType => "Forbidden.htmlx"; // Custom error type for forbidden access
        public ForbiddenCustomException(string message) : base(message) // Default status code is 403
        {
        }
    }
}
