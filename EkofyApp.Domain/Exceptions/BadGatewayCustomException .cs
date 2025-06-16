namespace EkofyApp.Domain.Exceptions
{
    public class BadGatewayCustomException : BaseException
    {
        public override int StatusCode => 502; // Default status code for bad gateway
        public override string ErrorType => "BadGateway.htmlx"; // Custom error type for bad gateway
        public BadGatewayCustomException(string message) : base(message) // Default status code is 502
        {
        }
    }
}
