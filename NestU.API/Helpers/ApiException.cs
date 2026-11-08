namespace NestU.API.Helpers;

/// <summary>Service ném ra, middleware đổi thành ProblemDetails với đúng mã HTTP.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;

    public static ApiException BadRequest(string m) => new(StatusCodes.Status400BadRequest, m);
    public static ApiException Forbidden(string m = "Bạn không có quyền thực hiện thao tác này.") => new(StatusCodes.Status403Forbidden, m);
    public static ApiException NotFound(string m = "Không tìm thấy dữ liệu.") => new(StatusCodes.Status404NotFound, m);
    public static ApiException Conflict(string m) => new(StatusCodes.Status409Conflict, m);
}

public class BusinessRuleException : ApiException
{
    public BusinessRuleException(string message) : base(StatusCodes.Status400BadRequest, message) { }
}
