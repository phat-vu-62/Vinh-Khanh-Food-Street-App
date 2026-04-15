using System.Security.Claims;
using FoodStreetApp.Shared.Entities;
using FoodStreetApp.Shared.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodStreetApp.CMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CmsDbContext _context;

    public AuthController(CmsDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return LocalRedirect("/login?error=true");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("userId", user.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (user.Role == "admin") return LocalRedirect("/admin");
        if (user.Role == "owner") return LocalRedirect("/owner");

        return LocalRedirect("/");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/login");
    }

    // ─── Mobile App JSON Endpoints ───────────────────────────────────

    public record MobileLoginRequest(string Username, string Password);
    public record MobileRegisterRequest(string Username, string Password, string? Email, string? FullName);

    [HttpPost("api-login")]
    public async Task<IActionResult> ApiLogin([FromBody] MobileLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { success = false, message = "Vui lòng nhập tài khoản và mật khẩu." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { success = false, message = "Sai tài khoản hoặc mật khẩu." });

        return Ok(new
        {
            success = true,
            userId = user.Id.ToString(),
            username = user.Username,
            fullName = user.FullName ?? user.Username,
            role = user.Role
        });
    }

    [HttpPost("api-register")]
    public async Task<IActionResult> ApiRegister([FromBody] MobileRegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(new { success = false, message = "Vui lòng nhập tài khoản, mật khẩu và họ tên." });

        if (req.Password.Length < 6)
            return BadRequest(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

        var exists = await _context.Users.AnyAsync(u => u.Username == req.Username);
        if (exists)
            return Conflict(new { success = false, message = "Tên tài khoản đã tồn tại." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Email = req.Email,
            FullName = req.FullName,
            Role = "enduser",
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            userId = user.Id.ToString(),
            username = user.Username,
            message = "Đăng ký thành công!"
        });
    }
}
