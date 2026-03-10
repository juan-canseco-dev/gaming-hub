using GameHub.Application.Abstractions.Authentication;
using GameHub.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameHub.Infrastructure.Authentication.Jwt;

public class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _options;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtProvider(IOptions<JwtOptions> options,
        UserManager<ApplicationUser> userManager)
    {
        _options = options.Value;
        _userManager = userManager;
    }

    public async Task<string> GenerateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, userId.ToString()),
            new (JwtRegisteredClaimNames.PreferredUsername, user!.UserName!),
            new (JwtRegisteredClaimNames.Email, user!.Email!),
            new (ClaimTypes.GivenName, user!.Fullname!)
        };

       var signingCredentials = new SigningCredentials(
           new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey!)),
           SecurityAlgorithms.HmacSha256
       );

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            null,
            DateTime.UtcNow.AddDays(1),
            signingCredentials
        );

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return await Task.FromResult(tokenValue);
    }
}