using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameHub.Web.API.IntegrationTests.Helpers;

public static class TestJwtTokenGenerator
{
    public static string Generate(Guid userId, string email)
    {
        var token = new JwtSecurityToken(
            issuer: "http://localhost",
            audience: "http://localhost",
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email)
            ]
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
