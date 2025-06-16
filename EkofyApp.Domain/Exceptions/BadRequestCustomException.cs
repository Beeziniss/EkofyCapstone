namespace EkofyApp.Domain.Exceptions
{
    public class BadRequestCustomException : BaseException
    {
        public override int StatusCode => 400; // Default status code is 400
        public override string ErrorType => "BadRequest.htmlx"; // Custom error type for bad requests
        public BadRequestCustomException(string message) : base(message) // Default status code is 400
        {
        }
    }
}
