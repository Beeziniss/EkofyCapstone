namespace EkofyApp.Domain.Exceptions
{
    public class ValidationCustomException : BaseException
    {
        public override int StatusCode => 400; // Default status code is 400
        public ValidationCustomException(string message) : base(message) // Default status code is 400
        {
        }
    }
}
