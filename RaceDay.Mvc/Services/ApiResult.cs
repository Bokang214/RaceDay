namespace RaceDay.Mvc.Services;

/// <summary>Used as the result type of API calls that return no data (204 No Content, or a body we ignore).</summary>
public readonly record struct Unit;

/// <summary>The outcome of one call to the RaceDay API: the data, or a friendly error message.</summary>
public sealed class ApiResult<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public int StatusCode { get; private init; }
    public string Message { get; private init; } = string.Empty;

    /// <summary>Field-level validation messages from the API (field name -> messages).</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; private init; } = new Dictionary<string, string[]>();

    public bool IsUnauthorized => this.StatusCode == 401;
    public bool IsForbidden => this.StatusCode == 403;
    public bool IsNotFound => this.StatusCode == 404;
    public bool IsConflict => this.StatusCode == 409;

    public static ApiResult<T> Ok(T data, int statusCode = 200) => new()
    {
        Success = true,
        Data = data,
        StatusCode = statusCode
    };

    public static ApiResult<T> Fail(int statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null) => new()
    {
        Success = false,
        StatusCode = statusCode,
        Message = message,
        Errors = errors ?? new Dictionary<string, string[]>()
    };
}
