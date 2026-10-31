using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NestU.WebApp.Models.Common;

namespace NestU.WebApp.ApiClients;

/// <summary>
/// Lớp nền cho mọi typed client. Đổi ProblemDetails / ValidationProblemDetails từ API thành ApiResult,
/// để controller đổ vào ModelState hoặc TempData.
/// </summary>
public abstract class ApiClientBase
{
    protected readonly HttpClient Http;
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected ApiClientBase(HttpClient http) => Http = http;

    private const string ApiDownMessage = "Không kết nối được tới máy chủ API (https://localhost:7001). Hãy chạy NestU.API trước.";

    /// <summary>Gọi API; API không chạy hoặc timeout → ApiResult 503 thay vì ném ngoại lệ.</summary>
    private static async Task<ApiResult<T>> SafeAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try { return await ReadAsync<T>(await send()); }
        catch (HttpRequestException) { return ApiResult<T>.Fail(503, ApiDownMessage); }
        catch (TaskCanceledException) { return ApiResult<T>.Fail(504, "API phản hồi quá lâu, vui lòng thử lại."); }
    }

    private static async Task<ApiResult> SafeAsync(Func<Task<HttpResponseMessage>> send)
    {
        try { return await ReadAsync(await send()); }
        catch (HttpRequestException) { return ApiResult.Fail(503, ApiDownMessage); }
        catch (TaskCanceledException) { return ApiResult.Fail(504, "API phản hồi quá lâu, vui lòng thử lại."); }
    }

    protected Task<ApiResult<T>> GetAsync<T>(string url) => SafeAsync<T>(() => Http.GetAsync(url));
    protected Task<ApiResult<T>> PostAsync<T>(string url, object? body) => SafeAsync<T>(() => Http.PostAsJsonAsync(url, body, Json));
    protected Task<ApiResult<T>> PutAsync<T>(string url, object? body) => SafeAsync<T>(() => Http.PutAsJsonAsync(url, body, Json));
    protected Task<ApiResult> PostAsync(string url, object? body = null) => SafeAsync(() => Http.PostAsJsonAsync(url, body, Json));
    protected Task<ApiResult> PutAsync(string url, object? body = null) => SafeAsync(() => Http.PutAsJsonAsync(url, body, Json));
    protected Task<ApiResult> DeleteAsync(string url) => SafeAsync(() => Http.DeleteAsync(url));
    protected Task<ApiResult<T>> PostFormAsync<T>(string url, MultipartFormDataContent form) => SafeAsync<T>(() => Http.PostAsync(url, form));
    protected Task<ApiResult> PostFormAsync(string url, MultipartFormDataContent form) => SafeAsync(() => Http.PostAsync(url, form));

    protected static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode)
        {
            if (res.StatusCode == HttpStatusCode.NoContent) return ApiResult<T>.Ok(default!, 204);
            var data = await res.Content.ReadFromJsonAsync<T>(Json);
            return ApiResult<T>.Ok(data!, (int)res.StatusCode);
        }
        var (msg, errors) = await ReadProblemAsync(res);
        return ApiResult<T>.Fail((int)res.StatusCode, msg, errors);
    }

    protected static async Task<ApiResult> ReadAsync(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return ApiResult.Ok((int)res.StatusCode);
        var (msg, errors) = await ReadProblemAsync(res);
        return ApiResult.Fail((int)res.StatusCode, msg, errors);
    }

    private static async Task<(string message, Dictionary<string, string[]> errors)> ReadProblemAsync(HttpResponseMessage res)
    {
        var errors = new Dictionary<string, string[]>();
        var message = res.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.",
            HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
            HttpStatusCode.NotFound => "Không tìm thấy dữ liệu.",
            _ => "Có lỗi xảy ra, vui lòng thử lại."
        };
        try
        {
            var text = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text)) return (message, errors);
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) message = t.GetString()!;
            if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String) message = d.GetString()!;
            if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                {
                    var list = p.Value.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x != "").ToArray();
                    // API trả PascalCase hoặc camelCase; model Web App dùng PascalCase
                    var key = p.Name.Length > 0 ? char.ToUpperInvariant(p.Name[0]) + p.Name[1..] : p.Name;
                    errors[key] = list;
                }
                if (errors.Count > 0 && message == "One or more validation errors occurred.")
                    message = "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại.";
            }
        }
        catch { /* body không phải JSON */ }
        return (message, errors);
    }
}

public static class ApiResultExtensions
{
    /// <summary>Đổ lỗi API vào ModelState để asp-validation-for hiển thị (mục 2.8).</summary>
    public static void AddToModelState<T>(this ApiResult<T> result, ModelStateDictionary modelState)
    {
        foreach (var (field, msgs) in result.Errors)
            foreach (var m in msgs) modelState.AddModelError(field, m);
        if (result.Errors.Count == 0 && !string.IsNullOrEmpty(result.Message))
            modelState.AddModelError(string.Empty, result.Message);
    }

    public static void AddToModelState(this ApiResult result, ModelStateDictionary modelState)
    {
        foreach (var (field, msgs) in result.Errors)
            foreach (var m in msgs) modelState.AddModelError(field, m);
        if (result.Errors.Count == 0 && !string.IsNullOrEmpty(result.Message))
            modelState.AddModelError(string.Empty, result.Message);
    }
}
