namespace EkofyApp.Domain.Exceptions
{
    public class ForbiddenCustomException(string message) : BaseException(message)
    {
        public override int StatusCode => 403; // Default status code for forbidden access
        public override string ErrorType => "Forbidden.htmlx"; // Custom error type for forbidden access
    }
}
