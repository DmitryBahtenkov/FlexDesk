using System.Security.Claims;

namespace Helpdesk.Auth;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        app.MapGet("/api/profile", (ClaimsPrincipal user) =>
            Results.Ok(new
            {
                username = user.Identity?.Name ?? user.FindFirst("sub")?.Value,
                role = user.FindFirst(ClaimTypes.Role)?.Value
            }))
            .RequireAuthorization();
    }
}
