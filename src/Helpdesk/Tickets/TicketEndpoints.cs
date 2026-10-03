using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Helpdesk.Data;
using StackExchange.Redis;

namespace Helpdesk.Tickets;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        app.MapPost("/tickets", (TicketRequest req, IDbConnection db, ClaimsPrincipal user) =>
        {
            var createdBy = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            db.Execute(
                "INSERT INTO Tickets (Subject, Body, CreatedAt, CreatedBy) VALUES (@Subject, @Body, @Now, @CreatedBy)",
                new { req.Subject, req.Body, Now = DateTime.UtcNow.ToString("o"), CreatedBy = createdBy });
            return Results.Redirect("/thanks.html");
        }).RequireAuthorization(p => p.RequireRole("guest", "agent"));

        app.MapGet("/api/my-tickets", (IDbConnection db, ClaimsPrincipal user) =>
        {
            var username = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return db.Query<Ticket>(
                "SELECT * FROM Tickets WHERE CreatedBy = @CreatedBy ORDER BY Id DESC",
                new { CreatedBy = username });
        }).RequireAuthorization(p => p.RequireRole("guest", "agent"));

        app.MapGet("/api/tickets", async (IDbConnection db, IConnectionMultiplexer redis) =>
        {
            var cache = redis.GetDatabase();
            var cached = await cache.StringGetAsync("cache:tickets");
            if (cached.HasValue)
                return Results.Text(cached!, "application/json");

            var tickets = db.Query<Ticket>("SELECT * FROM Tickets ORDER BY Id DESC").ToList();
            var json = JsonSerializer.Serialize(tickets);
            await cache.StringSetAsync("cache:tickets", json, TimeSpan.FromSeconds(5));
            return Results.Text(json, "application/json");
        }).RequireAuthorization(p => p.RequireRole("agent"));
    }
}
