namespace EkofyApp.Domain.Exceptions;
public sealed class TransactionOperationCustomException(string message) : BaseException(message)
{
    public override int StatusCode => 500;
    public override string ErrorType => "TransactionOperationCustomException.htmlx"; // Custom error type for invalid transaction operations
}
