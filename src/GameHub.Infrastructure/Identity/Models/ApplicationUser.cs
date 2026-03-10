using Microsoft.AspNetCore.Identity;

namespace GameHub.Infrastructure.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Fullname { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
