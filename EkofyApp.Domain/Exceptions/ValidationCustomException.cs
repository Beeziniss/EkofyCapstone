namespace EkofyApp.Domain.Exceptions
{
    public class ValidationCustomException(string message) : BaseException(message)
    {
        public override int StatusCode => 400; // Default status code is 400
    }
}
