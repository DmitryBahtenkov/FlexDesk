namespace Helpdesk.Partner;

public record PartnerTicketCreate(string Subject, string Body, string? Status);

public record PartnerTicketPatch(string? Subject, string? Body, string? Status);
