namespace EkofyApp.Domain.Exceptions
{
    public class BaseException(string message) : Exception(message)
    {
        public virtual int StatusCode { get; } = 500; // Default status code is 500
        public virtual string ErrorType => "BaseException.htmlx"; // Default error type
    }
}
