namespace Helpdesk.ApiKeys;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/api-keys", (IConfiguration config) =>
            Results.Ok(new
            {
                helpdeskApiKey = config["HelpdeskApiKey"]
            })).RequireAuthorization(p => p.RequireRole("agent"));
    }
}
