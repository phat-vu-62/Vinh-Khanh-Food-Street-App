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


