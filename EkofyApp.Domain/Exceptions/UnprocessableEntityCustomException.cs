namespace EkofyApp.Domain.Exceptions
{
    public class UnprocessableEntityCustomException : BaseException
    {
        public override int StatusCode => 422; // Default status code for unprocessable entity
        public override string ErrorType => "UnprocessableEntity.htmlx"; // Custom error type for unprocessable entity
        public UnprocessableEntityCustomException(string message) : base(message) // Default status code is 422
        {
        }
    }
}
