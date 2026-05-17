using VISSTA.Application.Interfaces;

namespace VISSTA.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private const string CartSessionKey = "VISSTA.SessionId";

    public string? UserId => accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string SessionId
    {
        get
        {
            var context = accessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");
            var sessionId = context.Session.GetString(CartSessionKey);
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId;
            }

            sessionId = Guid.NewGuid().ToString("N");
            context.Session.SetString(CartSessionKey, sessionId);
            return sessionId;
        }
    }
}
