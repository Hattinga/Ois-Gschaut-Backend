using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OisGschaut.API.Models;

namespace OisGschaut.API.Services;

public class JwtService(IConfiguration config)
{
    public string GenerateToken(User user)
    {
        var secret  = config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer  = config["Jwt:Issuer"]   ?? "OisGschaut";
        var audience = config["Jwt:Audience"] ?? "OisGschautClient";
        var expires  = int.TryParse(config["Jwt:ExpiresInDays"], out var days) ? days : 30;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,          user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName,   user.Username),
            new Claim(JwtRegisteredClaimNames.Email,        user.Email),
            new Claim("provider",                           user.OAuthProvider ?? "guest"),
            new Claim(JwtRegisteredClaimNames.Jti,          Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(expires),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Validate and return claims principal without throwing
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var secret = config["Jwt:Secret"];
        if (secret is null) return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer           = true,
                ValidIssuer              = config["Jwt:Issuer"] ?? "OisGschaut",
                ValidateAudience         = true,
                ValidAudience            = config["Jwt:Audience"] ?? "OisGschautClient",
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero,
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}
