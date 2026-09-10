using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppointmentBooking.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentBooking.Api.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly int _expirationMinutes;
    private readonly string _issuer;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
                  ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "1440");
        _issuer = configuration["Jwt:Issuer"] ?? "AppointmentBookingApi";
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
