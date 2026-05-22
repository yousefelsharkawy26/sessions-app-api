using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SessionApp.Infrastructure.Persistence;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SessionApp.API.Middleware;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;

    public UserActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        await _next(context);

        // Update LastSeenAt after request completes successfully if user is authenticated and not bypassed
        if (context.User.Identity?.IsAuthenticated == true && !context.Items.ContainsKey("BypassUserActivity"))
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        var now = DateTime.UtcNow;
                        // Avoid constant updates on every sub-second request (rate limit to once every 10 seconds or similar)
                        if (user.LastSeenAt == null || (now - user.LastSeenAt.Value).TotalSeconds > 10)
                        {
                            user.LastSeenAt = now;
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch
                {
                    // Fail silently so database connectivity issues don't crash requests
                }
            }
        }
    }
}
