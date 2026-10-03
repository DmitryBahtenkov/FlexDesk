using System.Data;
using Dapper;
using Helpdesk.Data;
using StackExchange.Redis;

namespace Helpdesk.Partner;

public static class PartnerTicketEndpoints
{
    public static void MapPartnerTicketEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/tickets", (HttpRequest req, IConfiguration config, IDbConnection db) =>
        {
            if (!ApiKeyGuard.IsValid(req, config))
                return Results.Unauthorized();
            var tickets = db.Query<Ticket>("SELECT * FROM Tickets ORDER BY Id DESC");
            return Results.Json(tickets);
        });

        app.MapGet("/api/v1/tickets/{id:long}", (long id, HttpRequest req, IConfiguration config, IDbConnection db) =>
        {
            if (!ApiKeyGuard.IsValid(req, config))
                return Results.Unauthorized();
            var ticket = db.QuerySingleOrDefault<Ticket>("SELECT * FROM Tickets WHERE Id = @Id", new { Id = id });
            return ticket is null ? Results.NotFound() : Results.Json(ticket);
        });

        app.MapPost("/api/v1/tickets", async (PartnerTicketCreate body, HttpRequest req, IConfiguration config,
            IDbConnection db, IConnectionMultiplexer redis) =>
        {
            if (!ApiKeyGuard.IsValid(req, config))
                return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(body.Subject) || string.IsNullOrWhiteSpace(body.Body))
                return Results.BadRequest();

            var status = string.IsNullOrWhiteSpace(body.Status) ? "open" : body.Status.Trim();
            db.Execute(
                "INSERT INTO Tickets (Subject, Body, Status, CreatedAt, CreatedBy) VALUES (@Subject, @Body, @Status, @Now, @CreatedBy)",
                new
                {
                    Subject = body.Subject.Trim(),
                    Body = body.Body,
                    Status = status,
                    Now = DateTime.UtcNow.ToString("o"),
                    CreatedBy = "partner-api"
                });
            var id = db.ExecuteScalar<long>("SELECT last_insert_rowid()");
            await redis.GetDatabase().KeyDeleteAsync("cache:tickets");
            var created = db.QuerySingle<Ticket>("SELECT * FROM Tickets WHERE Id = @Id", new { Id = id });
            return Results.Created($"/api/v1/tickets/{id}", created);
        });

        app.MapPatch("/api/v1/tickets/{id:long}", async (long id, PartnerTicketPatch patch, HttpRequest req,
            IConfiguration config, IDbConnection db, IConnectionMultiplexer redis) =>
        {
            if (!ApiKeyGuard.IsValid(req, config))
                return Results.Unauthorized();
            var existing = db.QuerySingleOrDefault<Ticket>("SELECT * FROM Tickets WHERE Id = @Id", new { Id = id });
            if (existing is null)
                return Results.NotFound();

            var subject = patch.Subject ?? existing.Subject;
            var ticketBody = patch.Body ?? existing.Body;
            var status = patch.Status ?? existing.Status;
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(ticketBody))
                return Results.BadRequest();

            db.Execute(
                "UPDATE Tickets SET Subject = @Subject, Body = @Body, Status = @Status WHERE Id = @Id",
                new { Id = id, Subject = subject, Body = ticketBody, Status = status });
            await redis.GetDatabase().KeyDeleteAsync("cache:tickets");
            var updated = db.QuerySingle<Ticket>("SELECT * FROM Tickets WHERE Id = @Id", new { Id = id });
            return Results.Json(updated);
        });

        app.MapDelete("/api/v1/tickets/{id:long}", async (long id, HttpRequest req, IConfiguration config,
            IDbConnection db, IConnectionMultiplexer redis) =>
        {
            if (!ApiKeyGuard.IsValid(req, config))
                return Results.Unauthorized();
            var n = db.Execute("DELETE FROM Tickets WHERE Id = @Id", new { Id = id });
            if (n == 0)
                return Results.NotFound();
            await redis.GetDatabase().KeyDeleteAsync("cache:tickets");
            return Results.NoContent();
        });
    }
}
