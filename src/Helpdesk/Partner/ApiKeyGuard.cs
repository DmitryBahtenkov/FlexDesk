namespace Helpdesk.Partner;

public static class ApiKeyGuard
{
    public static bool IsValid(HttpRequest req, IConfiguration config) =>
        req.Headers["X-Api-Key"] == config["HelpdeskApiKey"];
}
