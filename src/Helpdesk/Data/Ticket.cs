namespace Helpdesk.Data;

public record Ticket(long Id, string Subject, string Body, string Status, string CreatedAt, string? CreatedBy);
