using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Helpdesk.Data;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login", (LoginRequest req, IDbConnection db, IConfiguration config) =>
        {
            var user = db.QueryFirstOrDefault<User>(
                "SELECT * FROM Users WHERE Username = @Username",
                new { req.Username });

            if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        });
    }
}
