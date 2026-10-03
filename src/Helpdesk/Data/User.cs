namespace Helpdesk.Data;

public record User(long Id, string Username, string PasswordHash, string Role);
