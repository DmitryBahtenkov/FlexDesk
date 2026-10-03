namespace Helpdesk.Data;

public record Attachment(long Id, long TicketId, string OriginalName, string StoredPath);
