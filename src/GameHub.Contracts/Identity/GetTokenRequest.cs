using System.ComponentModel.DataAnnotations;

namespace GameHub.Contracts.Identity;

public class GetTokenRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(50)]
    public string Email { get; set; } = default!;

    [Required]
    [MaxLength(30)]
    public string Password { get; set; } = default!;
}
