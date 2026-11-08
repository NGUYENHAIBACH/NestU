using System.Net.Http.Headers;

namespace NestU.WebApp.Handlers;

/// <summary>Gắn JWT (lưu trong claim "access_token" của cookie) vào mọi request tới API.</summary>
public class JwtHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _http;
    public JwtHandler(IHttpContextAccessor http) => _http = http;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = _http.HttpContext?.User.FindFirst("access_token")?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (_http.HttpContext?.Items[VisitorCookieMiddleware.ItemKey] is string visitorId)
            request.Headers.TryAddWithoutValidation("X-Visitor-Id", visitorId);
        return base.SendAsync(request, ct);
    }
}
