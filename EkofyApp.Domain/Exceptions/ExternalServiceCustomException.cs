namespace EkofyApp.Domain.Exceptions
{
    public class ExternalServiceCustomException : BaseException
    {
        public override int StatusCode => 503; // Default status code for external service errors
        public ExternalServiceCustomException(string message) : base(message) // Default status code is 503
        {
        }
    }
}
