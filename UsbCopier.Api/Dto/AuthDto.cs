namespace UsbCopier.Api.Dto;

public class LoginRequest
{
    /// <summary>email или username — пользователь может вводить любое.</summary>
    public string EmailOrUsername { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsAdmin { get; set; }
}
