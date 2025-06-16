namespace EkofyApp.Domain.Exceptions
{
    public class ConflictCustomException : BaseException
    {
        public override int StatusCode => 409; // Default status code for conflict
        public override string ErrorType => "Conflict.htmlx"; // Custom error type for conflict
        public ConflictCustomException(string message) : base(message) // Default status code is 409
        {
        }
    }
}
