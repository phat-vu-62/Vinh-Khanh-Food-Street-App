using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FoodStreetApp.Shared.Context;
using FoodStreetApp.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FoodStreetApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CmsDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(CmsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [HttpPost("api-login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request null" });

        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "Missing username or password" });

        // DEBUG LOG
        Console.WriteLine($"[DEBUG] Login attempt: {request.Username}");

        // Hardcoded logic for debugging - Case-insensitive username check
        if (string.Equals(request.Username, "admin", StringComparison.OrdinalIgnoreCase) && request.Password == "123456")
        {
            Console.WriteLine("[DEBUG] Hardcoded ADMIN success");
            return Ok(new
            {
                success = true,
                message = "Đăng nhập thành công",
                userId = Guid.NewGuid().ToString(),
                token = "fake-jwt-token-" + Guid.NewGuid().ToString(),
                role = "Admin",
                username = "admin"
            });
        }
        
        if (string.Equals(request.Username, "owner", StringComparison.OrdinalIgnoreCase) && request.Password == "123456")
        {
            Console.WriteLine("[DEBUG] Hardcoded OWNER success");
            return Ok(new
            {
                success = true,
                message = "Đăng nhập thành công",
                userId = Guid.NewGuid().ToString(),
                token = "fake-jwt-token-" + Guid.NewGuid().ToString(),
                role = "Owner",
                username = "owner"
            });
        }

        // Fallback to database check
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
            
            Console.WriteLine($"[DEBUG] DB Lookup for {request.Username}: {(user != null ? "FOUND" : "NOT FOUND")}");
            
            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                Console.WriteLine($"[DEBUG] Password Verify: {isValid}");
                
                if (isValid)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Đăng nhập thành công",
                        userId = user.Id.ToString(),
                        token = GenerateJwtToken(user),
                        role = user.Role == "admin" ? "Admin" : (user.Role == "owner" ? "Owner" : user.Role),
                        username = user.Username,
                        fullName = user.FullName
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH-DB-ERROR] {ex.Message}");
        }

        return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
    }

    [HttpPost("register")]
    [HttpPost("api-register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request null" });

        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ Họ tên, tài khoản và mật khẩu." });

        if (password.Length < 6)
            return BadRequest(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

        var usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        if (usernameExists)
            return BadRequest(new { success = false, message = "Tên tài khoản đã tồn tại." });

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
            if (emailExists)
                return BadRequest(new { success = false, message = "Email đã được sử dụng." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Owner",
            FullName = fullName,
            Email = email,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Đăng ký thành công!"
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("userId", user.Id.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }
}
