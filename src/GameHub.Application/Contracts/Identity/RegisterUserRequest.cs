using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.Contracts.Identity;

public class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(50)]
    public string Email { get; set; } = default!;

    [Required]
    [MaxLength(30)]
    public string Username { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    public string Fullname { get; set; } = default!;

    [Required]
    [MinLength(6)]
    [MaxLength(30)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = default!;
}