using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SessionApp.Application.Common.Interfaces;
using SessionApp.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SessionApp.Infrastructure.Services;

public class JwtTokenGenerator(IConfiguration _configuration) : IJwtTokenGenerator
{
    public string GenerateToken(ApplicationUser user)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? "AntigravitySuperSecretKeyWhichMustBeAtLeast32BytesLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "SessionAppAPI";
        var audience = _configuration["JwtSettings:Audience"] ?? "SessionAppUsers";
        var expiryMinutes = double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "1440"); // 1 day

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
