namespace Application.ApiResult;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ErrorType ErrorType { get; private set; }

    public static Result<T> Success(T data)
    {
        return new Result<T> { IsSuccess = true, Data = data };
    }

    public static Result<T> Fail(string message, ErrorType type)
    {
        return new Result<T> { IsSuccess = false, ErrorMessage = message, ErrorType = type };
    }
}
