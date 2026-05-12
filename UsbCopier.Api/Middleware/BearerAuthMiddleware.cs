using Microsoft.EntityFrameworkCore;
using UsbCopier.Api.Data;

namespace UsbCopier.Api.Middleware;

/// <summary>
/// Читает заголовок Authorization: Bearer &lt;token&gt;, ищет токен в таблице
/// sessions. Если токен валиден и не истёк — кладёт UserId и IsAdmin в
/// HttpContext.Items. Контроллеры читают эти значения и фильтруют свои данные.
///
/// Анонимные запросы (без Bearer) проходят — контроллеры сами решают,
/// требовать ли авторизацию.
/// </summary>
public class BearerAuthMiddleware
{
    private readonly RequestDelegate _next;
    public BearerAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, UsbCopierDbContext db)
    {
        var header = ctx.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header.Substring(7).Trim();
            if (!string.IsNullOrEmpty(token))
            {
                var now = DateTime.UtcNow;
                var session = await db.Sessions
                    .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > now);
                if (session is not null)
                {
                    ctx.Items["UserId"] = session.UserId;
                    var user = await db.Users.FindAsync(session.UserId);
                    if (user is not null)
                    {
                        ctx.Items["IsAdmin"] = user.IsAdmin;
                        ctx.Items["Username"] = user.Username;
                    }
                }
            }
        }
        await _next(ctx);
    }
}

/// <summary>Утилиты для контроллеров — извлечь UserId/IsAdmin из HttpContext.</summary>
public static class AuthContextExtensions
{
    public static int? CurrentUserId(this HttpContext ctx)
        => ctx.Items["UserId"] as int?;

    public static bool IsAdmin(this HttpContext ctx)
        => ctx.Items["IsAdmin"] as bool? ?? false;
}
