using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using UsbCopier.Api.Data;
using UsbCopier.Api.Dto;
using UsbCopier.Api.Middleware;
using UsbCopier.Api.Models;
using UsbCopier.Api.Services;

namespace UsbCopier.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UsbCopierDbContext _db;
    public AuthController(UsbCopierDbContext db) => _db = db;

    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex =
        new(@"^[A-Za-z0-9_\-]+$", RegexOptions.Compiled);

    // ── POST /api/auth/register ────────────────────────────────────────
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var error = ValidateRegistration(req);
        if (error is not null) return BadRequest(new { error });

        // Email и Username уникальные — проверяем заранее, чтобы дать
        // понятную ошибку вместо MySQL violation.
        var emailLower = req.Email.Trim().ToLowerInvariant();
        var usernameLower = req.Username.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailLower))
            return Conflict(new { error = "Пользователь с таким email уже зарегистрирован" });

        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == usernameLower))
            return Conflict(new { error = "Пользователь с таким именем уже зарегистрирован" });

        var user = new User
        {
            Email = req.Email.Trim(),
            Username = req.Username.Trim(),
            PasswordHash = PasswordHasher.Hash(req.Password),
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var auth = await CreateSessionAsync(user);
        return Ok(auth);
    }

    // ── POST /api/auth/login ───────────────────────────────────────────
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.EmailOrUsername))
            return BadRequest(new { error = "Введите email или имя пользователя" });
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Введите пароль" });

        var ident = req.EmailOrUsername.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == ident || u.Username.ToLower() == ident);

        // Не разглашаем что именно не так — даём общую ошибку, как и
        // принято на нормальных входах.
        if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Неверный email/имя или пароль" });

        var auth = await CreateSessionAsync(user);
        return Ok(auth);
    }

    // ── POST /api/auth/logout ──────────────────────────────────────────
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header.Substring(7).Trim();
            var session = await _db.Sessions.FindAsync(token);
            if (session is not null)
            {
                _db.Sessions.Remove(session);
                await _db.SaveChangesAsync();
            }
        }
        return NoContent();
    }

    // ── GET /api/auth/me ───────────────────────────────────────────────
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = HttpContext.CurrentUserId();
        if (userId is null) return Unauthorized();
        var u = await _db.Users.FindAsync(userId.Value);
        if (u is null) return Unauthorized();
        return Ok(new UserDto
        {
            UserId = u.UserId, Username = u.Username, Email = u.Email, IsAdmin = u.IsAdmin
        });
    }

    // ── Хелперы ────────────────────────────────────────────────────────
    private async Task<AuthResponse> CreateSessionAsync(User user)
    {
        var session = new Session
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.UserId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return new AuthResponse
        {
            Token = session.Token,
            ExpiresAt = session.ExpiresAt,
            User = new UserDto
            {
                UserId = user.UserId, Username = user.Username,
                Email = user.Email,   IsAdmin = user.IsAdmin
            }
        };
    }

    private static string? ValidateRegistration(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email)) return "Email обязателен";
        if (!EmailRegex.IsMatch(req.Email.Trim()))
            return "Email должен иметь вид name@domain.tld";

        if (string.IsNullOrWhiteSpace(req.Username)) return "Имя пользователя обязательно";
        if (req.Username.Trim().Length < 3) return "Имя пользователя должно быть не короче 3 символов";
        if (!UsernameRegex.IsMatch(req.Username.Trim()))
            return "Имя пользователя — только латиница, цифры, _ и -";

        return ValidatePassword(req.Password);
    }

    public static string? ValidatePassword(string? pwd)
    {
        if (string.IsNullOrEmpty(pwd) || pwd.Length < 8)
            return "Пароль должен быть не короче 8 символов";
        if (!pwd.Any(c => c >= 'a' && c <= 'z'))
            return "Пароль должен содержать строчную латинскую букву";
        if (!pwd.Any(c => c >= 'A' && c <= 'Z'))
            return "Пароль должен содержать заглавную латинскую букву";
        if (!pwd.Any(char.IsDigit))
            return "Пароль должен содержать цифру";
        if (!pwd.Any(c => !char.IsLetterOrDigit(c)))
            return "Пароль должен содержать спецсимвол";
        return null;
    }
}
