namespace EkofyApp.Domain.Exceptions
{
    public class BaseException : Exception
    {
        public virtual int StatusCode { get; } = 500; // Default status code is 500
        public virtual string ErrorType => "BaseException.htmlx"; // Default error type
        public BaseException(string message) : base(message) // Default status code is 500
        {
        }
    }
}
