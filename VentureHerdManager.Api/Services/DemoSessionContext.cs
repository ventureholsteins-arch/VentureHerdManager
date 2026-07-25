using System.Text.RegularExpressions;

namespace VentureHerdManager.Api.Services;

public sealed partial class DemoSessionContext
{
    public const string HeaderName = "X-Demo-Session";
    public const int MaxSessionIdLength = 64;
    public const string LegacySessionId = "legacy-demo-session";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public DemoSessionContext(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        IsDemoMode = configuration.GetValue<bool>("DemoMode:Enabled");
    }

    public bool IsDemoMode { get; }

    public string? SessionId
    {
        get
        {
            if (!IsDemoMode)
            {
                return null;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            var value = httpContext.Request.Headers[HeaderName].FirstOrDefault();
            return IsValidSessionId(value) ? value : LegacySessionId;
        }
    }

    public static bool IsValidSessionId(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxSessionIdLength
        && SessionIdPattern().IsMatch(value);

    [GeneratedRegex("^[A-Za-z0-9_-]{16,64}$")]
    private static partial Regex SessionIdPattern();
}
