using System.Data;
using System.Security.Claims;
using System.Text;
using Helpdesk.ApiKeys;
using Helpdesk.Attachments;
using Helpdesk.Auth;
using Helpdesk.Data;
using Helpdesk.Partner;
using Helpdesk.Tickets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration["Database:Path"] ?? "helpdesk.db";
var uploadsRoot = builder.Configuration["Uploads:Root"] ?? Path.Combine(AppContext.BaseDirectory, "uploads");

Directory.CreateDirectory(Path.Combine(uploadsRoot, "1"));
var seedAttachmentPath = Path.Combine(uploadsRoot, "1", "invoice.pdf");
if (!File.Exists(seedAttachmentPath))
{
    File.WriteAllText(seedAttachmentPath, "Sample invoice content.");
}

builder.Services.AddScoped<IDbConnection>(_ =>
{
    var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();
    return connection;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });
builder.Services.AddAuthorization();
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["Server"] = "FlexDesk/1.4.2";
    await next();
});

// The evaluation guest account gets a short, generated starter password
// (e.g. "guest482"). Rotate or disable it before any real deployment.
var guestPasswordSuffix = Random.Shared.Next(100, 1000).ToString();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    DbInitializer.Initialize(db, guestPasswordSuffix);
}

app.Logger.LogInformation("Guest starter password: guest{Suffix}", guestPasswordSuffix);

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapTicketEndpoints();
app.MapPartnerTicketEndpoints();
app.MapApiKeyEndpoints();
app.MapAttachmentEndpoints(uploadsRoot);

app.MapGet("/robots.txt", () =>
    Results.Text(
        "User-agent: *\nDisallow: /agent/\n",
        "text/plain"));

app.UseStaticFiles();

app.MapGet("/agent", (IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "agent", "index.html"), "text/html"));

app.MapGet("/my", (IWebHostEnvironment env) =>
    Results.File(Path.Combine(env.WebRootPath, "my", "index.html"), "text/html"));

app.MapGet("/openapi.json", () =>
    Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.json"), "application/json"));

app.Run();

public partial class Program { }
