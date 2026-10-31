namespace NestU.WebApp.Models.Common;

/// <summary>Kết quả gọi API: thành công có Data, thất bại có mã HTTP + thông báo + lỗi từng trường.</summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public Dictionary<string, string[]> Errors { get; init; } = new();

    public static ApiResult<T> Ok(T data, int status = 200) => new() { Success = true, StatusCode = status, Data = data };
    public static ApiResult<T> Fail(int status, string? message, Dictionary<string, string[]>? errors = null)
        => new() { Success = false, StatusCode = status, Message = message, Errors = errors ?? new() };
}

public class ApiResult : ApiResult<object?>
{
    public static ApiResult Ok(int status = 204) => new() { Success = true, StatusCode = status };
    public static new ApiResult Fail(int status, string? message, Dictionary<string, string[]>? errors = null)
        => new() { Success = false, StatusCode = status, Message = message, Errors = errors ?? new() };
}
