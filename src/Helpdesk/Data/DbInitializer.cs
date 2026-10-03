using System.Data;
using Dapper;
using Helpdesk.Auth;

namespace Helpdesk.Data;

public static class DbInitializer
{
    public static void Initialize(IDbConnection db, string? guestPasswordSuffix = null)
    {
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Users (
              Id INTEGER PRIMARY KEY,
              Username TEXT UNIQUE,
              PasswordHash TEXT,
              Role TEXT
            );
            CREATE TABLE IF NOT EXISTS Tickets (
              Id INTEGER PRIMARY KEY,
              Subject TEXT,
              Body TEXT,
              Status TEXT DEFAULT 'open',
              CreatedAt TEXT,
              CreatedBy TEXT
            );
            CREATE TABLE IF NOT EXISTS Attachments (
              Id INTEGER PRIMARY KEY,
              TicketId INTEGER,
              OriginalName TEXT,
              StoredPath TEXT
            );
            """);

        var userCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM Users");
        if (userCount == 0)
        {
            var guestPassword = "guest" + (guestPasswordSuffix ?? DevSeedSecrets.GuestPasswordSuffix);
            db.Execute(
                "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role)",
                new object[]
                {
                    new { Username = "guest", PasswordHash = PasswordHasher.Hash(guestPassword), Role = "guest" },
                    new { Username = "agent01", PasswordHash = PasswordHasher.Hash(DevSeedSecrets.AgentPassword), Role = "agent" }
                });
        }

        var ticketCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM Tickets");
        if (ticketCount == 0)
        {
            db.Execute(
                "INSERT INTO Tickets (Id, Subject, Body, CreatedAt) VALUES (1, @Subject, @Body, @Now)",
                new { Subject = "Cannot access VPN", Body = "VPN client fails to connect since yesterday.", Now = DateTime.UtcNow.ToString("o") });

            db.Execute(
                "INSERT INTO Attachments (TicketId, OriginalName, StoredPath) VALUES (1, @OriginalName, @StoredPath)",
                new { OriginalName = "invoice.pdf", StoredPath = "1/invoice.pdf" });
        }
    }
}
