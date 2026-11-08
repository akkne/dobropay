namespace Dobropay.DeveloperApi.Models;

public sealed record DeveloperOperationResult<T>(
    bool IsSuccess,
    T? Value = default,
    int StatusCode = 200,
    DeveloperErrorResponse? Error = null)
{
    public static DeveloperOperationResult<T> Success(T value, int statusCode = 200) =>
        new(true, Value: value, StatusCode: statusCode);

    public static DeveloperOperationResult<T> Failure(int statusCode, string errorCode, string? message = null) =>
        new(false, StatusCode: statusCode, Error: new DeveloperErrorResponse(errorCode, message));
}
