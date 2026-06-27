namespace Control_OS_Lunix.Backend.Results;

public class Result
{
    protected Result(bool isSuccess, string errorCode, string message)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Message = message;
    }

    public bool IsSuccess { get; }

    public string ErrorCode { get; }

    public string Message { get; }

    public static Result Success(string message = "")
    {
        return new Result(true, string.Empty, message);
    }

    public static Result Failure(string errorCode, string message)
    {
        return new Result(false, errorCode, message);
    }
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, string errorCode, string message, T? value)
        : base(isSuccess, errorCode, message)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T? value, string message = "")
    {
        return new Result<T>(true, string.Empty, message, value);
    }

    public static new Result<T> Failure(string errorCode, string message)
    {
        return new Result<T>(false, errorCode, message, default);
    }
}
