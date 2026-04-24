using FoodStreetApp.CMS.Interfaces;
using FoodStreetApp.Shared.Context;
using FoodStreetApp.CMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

// Disable file system watchers to avoid inotify limit on Render/Linux
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");

var builder = WebApplication.CreateBuilder(args);

// Fix status 134 on Render/Linux by disabling file system watchers
builder.Configuration.AddEnvironmentVariables();

// Configure Port & Protocol at builder stage
var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5003");
builder.WebHost.ConfigureKestrel(options => {
    options.ListenAnyIP(port, listenOptions => {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});
Console.WriteLine($"[STARTUP] Kestrel listening on Port {port} (Any IP, HTTP/1.1 only)");

var connectionString = builder.Configuration.GetConnectionString("Postgres");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':', 2);

    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port,
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        Username = userInfo[0],
        Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
        SslMode = SslMode.Require
    };

    connectionString = npgsqlBuilder.ConnectionString;
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<CmsDbContext>(options => 
{
    options.UseNpgsql(connectionString, x => x.MigrationsAssembly("FoodStreetApp.CMS"));
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});
// Factory for concurrent operations (dashboard timer, API pings, etc.)
builder.Services.AddDbContextFactory<CmsDbContext>(options =>
{
    options.UseNpgsql(connectionString, x => x.MigrationsAssembly("FoodStreetApp.CMS"));
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
}, ServiceLifetime.Scoped);

// Register Gemini translation service
builder.Services.AddHttpClient<IGeminiTranslationService, GeminiTranslationService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Register Admin Services
builder.Services.AddScoped<IAdminDataService, AdminDataService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<ToastService>();

// Register IHttpClientFactory and Default HttpClient
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => 
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient();
    
    // If we're on Render, we use the public URL. Otherwise, we use localhost.
    var isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));
    var baseUrl = isRender 
        ? "https://vinh-khanh-food-street-app-iyhe.onrender.com/" 
        : $"http://localhost:{port}/";
    
    client.BaseAddress = new Uri(baseUrl);
    return client;
});

// Auth Services
builder.Services.AddAuthentication(defaultScheme: "Cookies")
    .AddCookie("Cookies");
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OwnerOnly", policy => policy.RequireRole("Owner"));
});

var app = builder.Build();

// Configure Forwarded Headers for Render Proxy transparency
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Global Request Logger
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        Console.WriteLine($"[API-REQUEST] {context.Request.Method} {context.Request.Path}");
    }
    await next();
});

app.MapGet("/health", () => "OK");

// Background Database Synchronization & Initialization
_ = Task.Run(async () => {
    try {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        Console.WriteLine("[DB-Background] Ensuring database created...");
        await dbContext.Database.EnsureCreatedAsync();
        
        await dbContext.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS ""PoiSyncActions"" (
                ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                ""PoiId"" integer NOT NULL,
                ""Action"" character varying(20) NOT NULL,
                ""OccurredAtUtc"" timestamp with time zone NOT NULL
            )");
        await dbContext.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PoiSyncActions_OccurredAtUtc"" ON ""PoiSyncActions"" (""OccurredAtUtc"")");
        await dbContext.Database.ExecuteSqlRawAsync(@"CREATE INDEX IF NOT EXISTS ""IX_PoiSyncActions_PoiId"" ON ""PoiSyncActions"" (""PoiId"")");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Pois"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" text");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""UserHistories"" ADD COLUMN IF NOT EXISTS ""DurationSeconds"" integer");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""UserHistories"" ADD COLUMN IF NOT EXISTS ""QRCode"" text");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""UserHistories"" ADD COLUMN IF NOT EXISTS ""Amount"" numeric");

        // Rename UserId → DeviceId (safe: only runs if old column still exists)
        await dbContext.Database.ExecuteSqlRawAsync(@"
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='UserHistories' AND column_name='UserId') THEN
                    ALTER TABLE ""UserHistories"" RENAME COLUMN ""UserId"" TO ""DeviceId"";
                END IF;
            END $$;
        ");

        // User profile columns
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""FullName"" text");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PhoneNumber"" text");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Email"" text");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""Address"" text");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""CreatedAtUtc"" timestamp with time zone DEFAULT now()");
        await dbContext.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsActive"" boolean DEFAULT true");

        // Explicit Admin Seeding
        var hasAdmin = await dbContext.Users.AnyAsync(u => u.Username == "admin");
        if (!hasAdmin)
        {
            Console.WriteLine("[DB-Background] Seeding admin user...");
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
            await dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Users\" (\"Id\", \"Username\", \"PasswordHash\", \"Role\") VALUES ({0}, {1}, {2}, {3})",
                Guid.NewGuid(), "admin", adminPasswordHash, "Admin"
            );
            Console.WriteLine("[DB-Background] Admin user seeded successfully.");
        }


    }
    catch (Exception ex) {
        Console.WriteLine($"[DB Error] Background startup sync failed: {ex.Message}");
    }
});

// ==========================================
// API ENDPOINTS (Mapped directly in CMS)
// ==========================================

// 1. AUTH LOGIN
app.MapPost("/api/auth/login", async (JsonElement body, CmsDbContext db, IConfiguration cfg) =>
{
    try
    {
        string? username = body.GetProperty("username").GetString();
        string? password = body.GetProperty("password").GetString();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return Results.BadRequest(new { message = "Username and password are required" });

        Console.WriteLine($"[AUTH] Login attempt for: {username}");

        // Hardcoded debug login
        if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) && password == "123456")
        {
            var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            return Results.Ok(new { 
                token = GenerateToken(adminUser?.Id.ToString() ?? Guid.Empty.ToString(), "admin", "Admin", cfg), 
                role = "Admin", 
                username = "admin" 
            });
        }


        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!user.IsActive)
                return Results.Json(new { message = "Tài khoản đã bị khóa. Liên hệ Admin để được hỗ trợ." }, statusCode: 403);

            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                // Normalize role string to Capitalized for the frontend
                var normalizedRole = user.Role;
                if (string.Equals(normalizedRole, "admin", StringComparison.OrdinalIgnoreCase)) normalizedRole = "Admin";
                if (string.Equals(normalizedRole, "owner", StringComparison.OrdinalIgnoreCase)) normalizedRole = "Owner";

                return Results.Ok(new { 
                    token = GenerateToken(user.Id.ToString(), user.Username, normalizedRole, cfg), 
                    role = normalizedRole, 
                    username = user.Username 
                });
            }
        }

        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AUTH-ERROR] {ex.Message}");
        return Results.Problem("Internal server error during login");
    }
});

string GenerateToken(string userId, string username, string role, IConfiguration cfg)
{
    var jwtSettings = cfg.GetSection("Jwt");
    var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);
    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim("userId", userId),
            new Claim("unique_name", username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}


// 2. POI & DATA ENDPOINTS
app.MapGet("/api/POI", (IAdminDataService service) => Results.Ok(service.GetPois()));
app.MapGet("/api/POI/{id:int}", (int id, IAdminDataService service) => {
    var item = service.GetPoiById(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
app.MapPost("/api/POI", (FoodStreetApp.Shared.Entities.POI poi, IAdminDataService service) => {
    var created = service.AddPoi(poi);
    return Results.Created($"/api/POI/{created.Id}", created);
});

// 3. SYNCHRONIZATION ENDPOINTS (For Mobile App)
app.MapGet("/api/sync/pois", async (CmsDbContext db) =>
{
    var pois = await db.Pois
        // .Where(p => p.IsApproved && p.IsActive) // Recommendation: only sync approved
        .ToListAsync();
    return Results.Ok(pois.Select(MapToSyncDto));
});

app.MapGet("/api/sync/poi-actions", async (string sinceUtc, CmsDbContext db) =>
{
    if (!DateTime.TryParse(sinceUtc, out var since)) return Results.BadRequest("Invalid date");
    var actions = await db.PoiSyncActions
        .Include(a => a.Poi)
        .Where(a => a.OccurredAtUtc > since.ToUniversalTime())
        .ToListAsync();

    return Results.Ok(actions.Select(a => new {
        a.PoiId,
        a.Action,
        a.OccurredAtUtc,
        Poi = a.Poi != null ? MapToSyncDto(a.Poi) : null
    }));
});

static object MapToSyncDto(FoodStreetApp.Shared.Entities.POI p) => new
{
    Id = p.Id,
    Name = p.Name,
    Latitude = p.Latitude,
    Longitude = p.Longitude,
    Radius = p.RadiusMeters,
    ApproachRadius = 200,
    Priority = p.Id,
    Rating = 4.5,
    ReviewCount = 0,
    Description = p.Description,
    AudioFile = p.AudioUrl,
    TtsText = p.TextContent,
    TtsTextEn = p.TextContentEn,
    TtsTextKo = p.TextContentKo,
    TtsTextZh = p.TextContentZh,
    TtsTextJa = p.TextContentJa,
    UseTts = !string.IsNullOrEmpty(p.TextContent),
    CooldownSeconds = 60,
    IsActive = p.IsActive && p.IsApproved, 
    ImageUrl = p.ImageUrl
};

// 4. USAGE HISTORY & TRACKING
app.MapPost("/api/history", async (FoodStreetApp.Shared.Entities.UserHistory history, CmsDbContext db) =>
{
    // Allow app_ping and qr_listen_ping (PoiId=0) for real-time online tracking
    if (history.PoiId <= 0 && history.Action != "app_ping" && history.Action != "qr_listen_ping")
        return Results.BadRequest("Invalid history data");
    if (string.IsNullOrEmpty(history.Action))
        return Results.BadRequest("Action required");

    history.VisitedAtUtc = DateTime.UtcNow;
    db.UserHistories.Add(history);
    await db.SaveChangesAsync();
    return Results.Ok(new { success = true });
});

// 4b. (Removed: Mobile app auth endpoints api-login & api-register - app is now anonymous)

app.MapGet("/api/Audio", (IAdminDataService service) => Results.Ok(service.GetAudios()));
app.MapGet("/api/Tour", (IAdminDataService service) => Results.Ok(service.GetTours()));

// ==========================================
// 5. MERCHANT REGISTRATION & USER MANAGEMENT
// ==========================================

// Public endpoint: Register a new merchant (owner)
app.MapPost("/api/auth/register-merchant", async (JsonElement body, CmsDbContext db) =>
{
    try
    {
        var fullName = body.GetProperty("fullName").GetString();
        var phone = body.GetProperty("phone").GetString();
        var email = body.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        var address = body.TryGetProperty("address", out var addrProp) ? addrProp.GetString() : null;
        var poiName = body.GetProperty("poiName").GetString();
        var amount = body.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 200000m;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(poiName))
            return Results.BadRequest(new { message = "Họ tên, SĐT và tên địa điểm là bắt buộc" });

        if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(\+84|0)[0-9]{8,10}$"))
            return Results.BadRequest(new { message = "Số điện thoại không hợp lệ" });

        if (!string.IsNullOrWhiteSpace(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return Results.BadRequest(new { message = "Email không đúng định dạng" });

        // Check if phone (username) already exists
        var exists = await db.Users.AnyAsync(u => u.Username.ToLower() == phone!.ToLower());
        if (exists)
            return Results.Conflict(new { message = "Số điện thoại đã được đăng ký" });

        // Create new POI
        var poi = new FoodStreetApp.Shared.Entities.POI
        {
            Name = poiName!,
            Description = $"Quán của {fullName}",
            Latitude = 10.7553,  // Default: Vinh Khánh street
            Longitude = 106.6933,
            IsActive = true,
            IsApproved = false,  // Needs admin approval
            Type = FoodStreetApp.Shared.Enums.POIType.Food
        };
        db.Pois.Add(poi);
        await db.SaveChangesAsync();

        // Create new User
        var user = new FoodStreetApp.Shared.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = phone!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
            Role = "Owner",
            FullName = fullName,
            PhoneNumber = phone,
            Email = email,
            Address = address,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        db.Users.Add(user);

        // Link POI to owner
        poi.OwnerId = user.Id;

        // Record payment history
        db.UserHistories.Add(new FoodStreetApp.Shared.Entities.UserHistory
        {
            DeviceId = user.Id.ToString(),
            PoiId = poi.Id,
            Action = "register_merchant",
            VisitedAtUtc = DateTime.UtcNow,
            Amount = amount
        });

        await db.SaveChangesAsync();
        Console.WriteLine($"[REGISTER] New merchant: {fullName} ({phone}), POI: {poiName}");

        return Results.Ok(new { success = true, username = phone, poiId = poi.Id, userId = user.Id });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[REGISTER-ERROR] {ex.Message}");
        return Results.Problem("Lỗi khi đăng ký: " + ex.Message);
    }
});

// Admin-only: Get all users
app.MapGet("/api/auth/users", async (CmsDbContext db) =>
{
    var users = await db.Users
        .OrderByDescending(u => u.CreatedAtUtc)
        .Select(u => new
        {
            u.Id, u.Username, u.FullName, u.PhoneNumber, u.Email,
            u.Address, u.Role, u.IsActive, u.CreatedAtUtc
        })
        .ToListAsync();
    return Results.Ok(users);
});

// Admin-only: Toggle user active status
app.MapPut("/api/auth/users/{id}/toggle-active", async (Guid id, CmsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    user.IsActive = !user.IsActive;
    await db.SaveChangesAsync();
    return Results.Ok(new { user.Id, user.IsActive });
});

// Admin-only: Update user info
app.MapPut("/api/auth/users/{id}", async (Guid id, JsonElement body, CmsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();

    var fullName = body.TryGetProperty("fullName", out var fnProp) && fnProp.ValueKind != JsonValueKind.Null ? fnProp.GetString() : user.FullName;
    var phone = body.TryGetProperty("phoneNumber", out var phProp) && phProp.ValueKind != JsonValueKind.Null ? phProp.GetString() : user.PhoneNumber;
    var email = body.TryGetProperty("email", out var emProp) && emProp.ValueKind != JsonValueKind.Null ? emProp.GetString() : user.Email;
    var password = body.TryGetProperty("password", out var pwProp) && pwProp.ValueKind != JsonValueKind.Null ? pwProp.GetString() : null;

    if (!string.IsNullOrWhiteSpace(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(\+84|0)[0-9]{8,10}$"))
        return Results.BadRequest(new { message = "Số điện thoại không hợp lệ" });

    if (!string.IsNullOrWhiteSpace(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        return Results.BadRequest(new { message = "Email không đúng định dạng" });

    user.FullName = fullName;
    user.PhoneNumber = phone;
    user.Email = email;

    if (!string.IsNullOrWhiteSpace(password))
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { success = true });
});

// Admin-only: Delete user
app.MapDelete("/api/auth/users/{id}", async (Guid id, CmsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    if (user.Role == "Admin") return Results.BadRequest(new { message = "Không thể xóa tài khoản Admin" });
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { success = true });
});

// Admin-only: Create a new owner user (no payment required)
app.MapPost("/api/auth/users", async (JsonElement body, CmsDbContext db) =>
{
    try
    {
        var username = body.GetProperty("username").GetString();
        var fullName = body.TryGetProperty("fullName", out var fnProp) ? fnProp.GetString() : null;
        var phone = body.TryGetProperty("phone", out var phProp) ? phProp.GetString() : null;
        var email = body.TryGetProperty("email", out var emProp) ? emProp.GetString() : null;
        var password = body.TryGetProperty("password", out var pwProp) ? pwProp.GetString() : "123";
        var role = body.TryGetProperty("role", out var rlProp) ? rlProp.GetString() : "Owner";

        if (string.IsNullOrWhiteSpace(username))
            return Results.BadRequest(new { message = "Username là bắt buộc" });

        if (!string.IsNullOrWhiteSpace(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(\+84|0)[0-9]{8,10}$"))
            return Results.BadRequest(new { message = "Số điện thoại không hợp lệ" });

        if (!string.IsNullOrWhiteSpace(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return Results.BadRequest(new { message = "Email không đúng định dạng" });

        var exists = await db.Users.AnyAsync(u => u.Username.ToLower() == username!.ToLower());
        if (exists)
            return Results.Conflict(new { message = "Username đã tồn tại" });

        var user = new FoodStreetApp.Shared.Entities.User
        {
            Id = Guid.NewGuid(),
            Username = username!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password ?? "123"),
            Role = role ?? "Owner",
            FullName = fullName,
            PhoneNumber = phone,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, user.Id, user.Username });
    }
    catch (Exception ex)
    {
        return Results.Problem("Lỗi tạo user: " + ex.Message);
    }
});

// Get ALL revenue stats (real data from DB)
// Get ALL revenue stats (real data from DB)
// Calculates revenue based on actual transactions
app.MapGet("/api/auth/revenue", async (CmsDbContext db) =>
{
    var all = await db.UserHistories.ToListAsync();

    // Registration revenue always counts
    var registerRevenue = all.Where(x => x.Action == "register_merchant" && x.Amount > 0).Sum(x => x.Amount ?? 0);

    // Listen revenue counts explicitly paid positive amounts
    var listenRevenue = all.Where(x => x.Action == "payment_listen" && x.Amount > 0).Sum(x => x.Amount ?? 0);

    // Create POI gross revenue includes ALL create POI payments (approved + pending)
    var createPoiGross = all.Where(x => x.Action == "payment_create_poi" && x.Amount > 0).Sum(x => x.Amount ?? 0);
    var createPoiRefunds = Math.Abs(all.Where(x => x.Action == "refund_create_poi" && x.Amount < 0).Sum(x => x.Amount ?? 0));
    var createPoiRevenue = createPoiGross - createPoiRefunds;

    // Total refund is the absolute sum of all negative transaction amounts
    var totalRefund = Math.Abs(all.Where(x => x.Amount < 0).Sum(x => x.Amount ?? 0));

    // Total Revenue is gross revenue MINUS total refunds
    var totalRevenue = registerRevenue + listenRevenue + createPoiGross - totalRefund;

    return Results.Ok(new
    {
        TotalRevenue = totalRevenue,
        RegisterRevenue = registerRevenue,
        ListenRevenue = listenRevenue,
        CreatePoiRevenue = createPoiRevenue,
        TotalRefund = totalRefund,
        TotalRegistrations = all.Count(x => x.Action == "register_merchant" && x.Amount > 0),
        TotalListenPayments = all.Count(x => x.Action == "payment_listen" && x.Amount > 0),
        TotalCreatePoiPayments = all.Count(x => x.Action == "payment_create_poi" && x.Amount > 0)
    });
});

// Gửi heartbeat từ CMS
app.MapPost("/api/auth/cms-ping", async (ClaimsPrincipal user, CmsDbContext db) =>
{
    var userIdStr = user.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
    if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    var history = new FoodStreetApp.Shared.Entities.UserHistory
    {
        DeviceId = userIdStr,
        Action = "cms_ping",
        VisitedAtUtc = DateTime.UtcNow
    };
    db.UserHistories.Add(history);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();
// Get list of currently online user IDs (app_ping, cms_ping, qr_listen_ping in last 6 seconds)
app.MapGet("/api/auth/online-users", async (CmsDbContext db) =>
{
    var threshold = DateTime.UtcNow.AddSeconds(-6);
    var onlineUserIds = await db.UserHistories.AsNoTracking()
        .Where(h => (h.Action == "app_ping" || h.Action == "cms_ping" || h.Action == "qr_listen_ping") && h.VisitedAtUtc >= threshold)
        .Select(h => h.DeviceId)
        .Distinct()
        .ToListAsync();
    return Results.Ok(onlineUserIds);
});

// Get online QR listeners count (separate from app users)
app.MapGet("/api/auth/online-qr-count", async (CmsDbContext db) =>
{
    var qrThreshold = DateTime.UtcNow.AddSeconds(-4);
    var count = await db.UserHistories.AsNoTracking()
        .Where(h => h.Action == "qr_listen_ping" && h.VisitedAtUtc >= qrThreshold)
        .Select(h => h.DeviceId)
        .Distinct()
        .CountAsync();
    return Results.Ok(new { count });
});

// Get owner-specific revenue (listen payments for their APPROVED POIs only)
app.MapGet("/api/auth/owner-revenue/{ownerId}", async (Guid ownerId, CmsDbContext db) =>
{
    var ownerApprovedPoiIds = await db.Pois
        .Where(p => p.OwnerId == ownerId && p.IsApproved)
        .Select(p => p.Id)
        .ToListAsync();

    var listenRevenue = await db.UserHistories
        .Where(h => h.Action == "payment_listen" && h.Amount.HasValue && ownerApprovedPoiIds.Contains(h.PoiId))
        .SumAsync(h => h.Amount ?? 0);

    var listenCount = await db.UserHistories
        .Where(h => h.Action == "payment_listen" && ownerApprovedPoiIds.Contains(h.PoiId))
        .CountAsync();

    return Results.Ok(new { ListenRevenue = listenRevenue, ListenCount = listenCount });
});

// ==========================================
// 6. PAYMENT GATEWAY (QR Listen + POI Create)
// ==========================================

// Check if device already paid for a POI
app.MapPost("/api/listen/check-payment", async (JsonElement body, CmsDbContext db) =>
{
    var deviceId = body.GetProperty("deviceId").GetString();
    var poiId = body.GetProperty("poiId").GetInt32();

    if (string.IsNullOrWhiteSpace(deviceId))
        return Results.BadRequest(new { paid = false });

    var paid = await db.UserHistories.AnyAsync(h =>
        h.DeviceId == deviceId && h.PoiId == poiId && h.Action == "payment_listen" && h.Amount.HasValue);

    return Results.Ok(new { paid });
});

// Record listen payment
app.MapPost("/api/listen/pay", async (JsonElement body, CmsDbContext db) =>
{
    try
    {
        var deviceId = body.GetProperty("deviceId").GetString();
        var poiId = body.GetProperty("poiId").GetInt32();
        var amount = body.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 20000m;

        if (string.IsNullOrWhiteSpace(deviceId))
            return Results.BadRequest(new { message = "Device ID bắt buộc" });

        // Check if already paid
        var alreadyPaid = await db.UserHistories.AnyAsync(h =>
            h.DeviceId == deviceId && h.PoiId == poiId && h.Action == "payment_listen");
        if (alreadyPaid)
            return Results.Ok(new { success = true, alreadyPaid = true });

        db.UserHistories.Add(new FoodStreetApp.Shared.Entities.UserHistory
        {
            DeviceId = deviceId!,
            PoiId = poiId,
            Action = "payment_listen",
            VisitedAtUtc = DateTime.UtcNow,
            Amount = amount
        });
        await db.SaveChangesAsync();
        Console.WriteLine($"[PAY-LISTEN] Device {deviceId} paid {amount}đ for POI {poiId}");

        return Results.Ok(new { success = true, alreadyPaid = false });
    }
    catch (Exception ex)
    {
        return Results.Problem("Lỗi thanh toán: " + ex.Message);
    }
});

// Record POI creation payment (Owner)
app.MapPost("/api/owner/pay-create-poi", async (JsonElement body, CmsDbContext db) =>
{
    try
    {
        var userId = body.GetProperty("userId").GetString();
        var amount = body.TryGetProperty("amount", out var amtProp) ? amtProp.GetDecimal() : 200000m;

        if (string.IsNullOrWhiteSpace(userId))
            return Results.BadRequest(new { message = "User ID bắt buộc" });

        db.UserHistories.Add(new FoodStreetApp.Shared.Entities.UserHistory
        {
            DeviceId = userId!,
            PoiId = 0, // Will be linked after POI creation
            Action = "payment_create_poi",
            VisitedAtUtc = DateTime.UtcNow,
            Amount = amount
        });
        await db.SaveChangesAsync();
        Console.WriteLine($"[PAY-POI] User {userId} paid {amount}đ to create new POI");

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return Results.Problem("Lỗi thanh toán: " + ex.Message);
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

