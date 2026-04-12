using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace FoodStreetApp.CMS.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IConfiguration _configuration;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _configuration = configuration;
    }

    public async Task<bool> Login(string username, string password)
    {
        var baseUrl = _configuration["Api:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:10000";
        var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/auth/login", new { username, password });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResult>();
            if (result != null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userRole", result.Role);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userId", result.UserId.ToString());
                
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
                return true;
            }
        }

        return false;
    }

    public async Task Logout()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userRole");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userId");
        
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UserId { get; set; }
}
