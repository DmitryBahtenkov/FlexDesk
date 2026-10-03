using Helpdesk.Partner;

namespace Helpdesk.Attachments;

public static class AttachmentEndpoints
{
    public static void MapAttachmentEndpoints(this WebApplication app, string uploadsRoot)
    {
        app.MapGet("/api/v1/tickets/{id}/attachments/download", (int id, string path, HttpRequest req, IConfiguration config) =>
        {
            if (!ApiKeyGuard.IsValid(req, config))
                return Results.Unauthorized();

            var filePath = Path.Combine(uploadsRoot, path);
            if (!File.Exists(filePath))
                return Results.NotFound();

            return Results.File(filePath);
        });
    }
}
