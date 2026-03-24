namespace GameHub.Web.UI.Authentication;

public class UserDetails
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Fullname { get; set; } = default!;
}
